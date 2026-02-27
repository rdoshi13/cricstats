using CricStats.Application.Interfaces;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Weather;
using CricStats.Contracts.Weather;
using CricStats.Domain.Entities;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure.Services.Weather;

public sealed class WeatherRiskService : IWeatherRiskService
{
    private readonly CricStatsDbContext _dbContext;
    private readonly IUpcomingMatchesSyncService _upcomingMatchesSyncService;
    private readonly IReadOnlyDictionary<string, IWeatherProvider> _weatherProviders;
    private readonly WeatherRiskOptions _options;
    private readonly ILogger<WeatherRiskService> _logger;
    private readonly bool _isTestingEnvironment;

    public WeatherRiskService(
        CricStatsDbContext dbContext,
        IUpcomingMatchesSyncService upcomingMatchesSyncService,
        IEnumerable<IWeatherProvider> weatherProviders,
        IOptions<WeatherRiskOptions> options,
        ILogger<WeatherRiskService> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _dbContext = dbContext;
        _upcomingMatchesSyncService = upcomingMatchesSyncService;
        _weatherProviders = weatherProviders.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
        _isTestingEnvironment = hostEnvironment?.IsEnvironment("Testing") ?? true;
    }

    public async Task<RefreshWeatherRiskResult> RefreshUpcomingWeatherRiskAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var windowEnd = now.AddDays(Math.Clamp(_options.RefreshWindowDays, 1, 30));

        var query = _dbContext.Matches
            .Include(x => x.Venue)
            .Include(x => x.MatchWeatherRisk)
            .Where(x => x.StartTimeUtc >= now && x.StartTimeUtc <= windowEnd);

        if (!_isTestingEnvironment)
        {
            query = query.Where(x =>
                !x.SourceProvider.StartsWith("Test") &&
                !x.SourceProvider.StartsWith("Fixture"));
        }

        var matches = await query
            .OrderBy(x => x.StartTimeUtc)
            .ToListAsync(cancellationToken);

        if (matches.Count == 0)
        {
            _logger.LogInformation("No upcoming matches for weather refresh. Triggering fixture sync.");
            await _upcomingMatchesSyncService.SyncUpcomingMatchesAsync(cancellationToken);

            var refreshedQuery = _dbContext.Matches
                .Include(x => x.Venue)
                .Include(x => x.MatchWeatherRisk)
                .Where(x => x.StartTimeUtc >= now && x.StartTimeUtc <= windowEnd);

            if (!_isTestingEnvironment)
            {
                refreshedQuery = refreshedQuery.Where(x =>
                    !x.SourceProvider.StartsWith("Test") &&
                    !x.SourceProvider.StartsWith("Fixture"));
            }

            matches = await refreshedQuery
                .OrderBy(x => x.StartTimeUtc)
                .ToListAsync(cancellationToken);
        }

        var provider = ResolveProvider();
        var refreshedAt = DateTimeOffset.UtcNow;
        var risksUpdated = 0;

        foreach (var match in matches)
        {
            var result = await ComputeAndPersistRiskForMatchAsync(match, provider, refreshedAt, cancellationToken);
            if (result is not null)
            {
                risksUpdated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshWeatherRiskResult(
            ProviderUsed: provider.Name,
            MatchesProcessed: matches.Count,
            RisksUpdated: risksUpdated,
            RefreshedAtUtc: refreshedAt);
    }

    public async Task<MatchWeatherRiskResponse?> GetMatchWeatherRiskAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Matches
            .Include(x => x.Venue)
            .Include(x => x.MatchWeatherRisk)
            .FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken);

        if (match is null)
        {
            return null;
        }

        var provider = ResolveProvider();
        var now = DateTimeOffset.UtcNow;

        var computation = await ComputeAndPersistRiskForMatchAsync(match, provider, now, cancellationToken)
            ?? await BuildComputationFromSnapshotsAsync(match, cancellationToken)
            ?? new WeatherRiskComputation(0m, Domain.Enums.RiskLevel.Low, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MatchWeatherRiskResponse(
            MatchId: match.Id,
            CompositeRiskScore: computation.CompositeRiskScore,
            RiskLevel: computation.RiskLevel.ToString(),
            ComputedAtUtc: match.MatchWeatherRisk?.ComputedAtUtc ?? now,
            Breakdown: new WeatherRiskBreakdownDto(
                computation.AveragePrecipProbability,
                computation.AveragePrecipAmount,
                computation.AverageHumidity,
                computation.AverageWindSpeed,
                computation.PrecipProbabilityContribution,
                computation.PrecipAmountContribution,
                computation.HumidityContribution,
                computation.WindSpeedContribution));
    }

    private IWeatherProvider ResolveProvider()
    {
        if (_weatherProviders.TryGetValue(_options.ProviderName, out var namedProvider))
        {
            return namedProvider;
        }

        var firstProvider = _weatherProviders.Values.FirstOrDefault();
        if (firstProvider is null)
        {
            throw new InvalidOperationException("No weather provider is registered.");
        }

        return firstProvider;
    }

    private async Task<WeatherRiskComputation?> ComputeAndPersistRiskForMatchAsync(
        Match match,
        IWeatherProvider provider,
        DateTimeOffset syncedAtUtc,
        CancellationToken cancellationToken)
    {
        var fromUtc = match.StartTimeUtc.AddHours(-2);
        var toUtc = match.StartTimeUtc.AddHours(6);

        var forecastPoints = await provider.GetForecastAsync(
            match.Venue.Latitude,
            match.Venue.Longitude,
            fromUtc,
            toUtc,
            cancellationToken);

        if (forecastPoints.Count == 0)
        {
            return null;
        }

        await UpsertForecastSnapshotsAsync(match.VenueId, provider.Name, forecastPoints, syncedAtUtc, cancellationToken);

        var computation = WeatherRiskCalculator.Compute(
            forecastPoints,
            _options.PrecipAmountMaxMm,
            _options.WindSpeedMaxKph);

        if (match.MatchWeatherRisk is null)
        {
            match.MatchWeatherRisk = new MatchWeatherRisk
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id
            };

            _dbContext.MatchWeatherRisks.Add(match.MatchWeatherRisk);
        }

        match.MatchWeatherRisk.CompositeRiskScore = computation.CompositeRiskScore;
        match.MatchWeatherRisk.RiskLevel = computation.RiskLevel;
        match.MatchWeatherRisk.ComputedAtUtc = syncedAtUtc;

        return computation;
    }

    private async Task UpsertForecastSnapshotsAsync(
        Guid venueId,
        string sourceProvider,
        IReadOnlyList<WeatherForecastPoint> forecastPoints,
        DateTimeOffset syncedAtUtc,
        CancellationToken cancellationToken)
    {
        var externalIds = forecastPoints.Select(x => x.ExternalId).ToList();

        var existing = (await _dbContext.WeatherSnapshots
            .Where(x => x.VenueId == venueId && x.SourceProvider == sourceProvider && externalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

        foreach (var forecast in forecastPoints)
        {
            if (!existing.TryGetValue(forecast.ExternalId, out var snapshot))
            {
                snapshot = new WeatherSnapshot
                {
                    Id = Guid.NewGuid(),
                    VenueId = venueId,
                    SourceProvider = sourceProvider,
                    ExternalId = forecast.ExternalId
                };

                _dbContext.WeatherSnapshots.Add(snapshot);
                existing[forecast.ExternalId] = snapshot;
            }

            snapshot.TimestampUtc = forecast.TimestampUtc;
            snapshot.Temperature = forecast.Temperature;
            snapshot.Humidity = forecast.Humidity;
            snapshot.WindSpeed = forecast.WindSpeed;
            snapshot.PrecipProbability = forecast.PrecipProbability;
            snapshot.PrecipAmount = forecast.PrecipAmount;
            snapshot.LastSyncedAtUtc = syncedAtUtc;
        }
    }

    private async Task<WeatherRiskComputation?> BuildComputationFromSnapshotsAsync(
        Match match,
        CancellationToken cancellationToken)
    {
        var fromUtc = match.StartTimeUtc.AddHours(-2);
        var toUtc = match.StartTimeUtc.AddHours(6);

        var snapshots = await _dbContext.WeatherSnapshots
            .Where(x => x.VenueId == match.VenueId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
            .Select(x => new WeatherForecastPoint(
                x.ExternalId,
                x.TimestampUtc,
                x.Temperature,
                x.Humidity,
                x.WindSpeed,
                x.PrecipProbability,
                x.PrecipAmount))
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
        {
            return null;
        }

        return WeatherRiskCalculator.Compute(
            snapshots,
            _options.PrecipAmountMaxMm,
            _options.WindSpeedMaxKph);
    }
}
