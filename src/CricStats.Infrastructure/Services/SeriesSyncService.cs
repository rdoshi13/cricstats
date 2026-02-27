using CricStats.Application.Interfaces;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models;
using CricStats.Domain.Entities;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure.Services;

public sealed class SeriesSyncService : ISeriesSyncService
{
    private readonly CricStatsDbContext _dbContext;
    private readonly IReadOnlyDictionary<string, ICricketProvider> _providers;
    private readonly CricketProvidersOptions _options;
    private readonly ILogger<SeriesSyncService> _logger;
    private readonly bool _isTestingEnvironment;

    public SeriesSyncService(
        CricStatsDbContext dbContext,
        IEnumerable<ICricketProvider> providers,
        IOptions<CricketProvidersOptions> options,
        ILogger<SeriesSyncService> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _dbContext = dbContext;
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
        _isTestingEnvironment = hostEnvironment?.IsEnvironment("Testing") ?? true;
    }

    public async Task<SeriesSyncResult> SyncUpcomingSeriesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var fromUtc = now.AddDays(-1);
        var toUtc = now.AddDays(Math.Clamp(_options.SeriesSyncWindowDays, 7, 180));

        var providerOrder = BuildProviderPriority();
        var providersTried = new List<string>();

        IReadOnlyList<Application.Models.Providers.ProviderSeries> selectedSeries = [];
        string? selectedProvider = null;

        foreach (var providerName in providerOrder)
        {
            if (!_isTestingEnvironment && IsTestProviderName(providerName))
            {
                continue;
            }

            providersTried.Add(providerName);
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                continue;
            }

            try
            {
                var upcomingSeries = await provider.GetUpcomingSeriesAsync(fromUtc, toUtc, cancellationToken);
                if (upcomingSeries.Count == 0)
                {
                    continue;
                }

                selectedSeries = upcomingSeries;
                selectedProvider = provider.Name;
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{ProviderName}' failed during series sync.", providerName);
            }
        }

        if (selectedProvider is null)
        {
            return new SeriesSyncResult(
                ProviderUsed: null,
                ProvidersTried: providersTried,
                SeriesUpserted: 0,
                SeriesMatchesUpserted: 0,
                SyncedAtUtc: now);
        }

        var providerUsed = _providers[selectedProvider];
        var seriesExternalIds = selectedSeries.Select(x => x.ExternalId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingSeries = (await _dbContext.Series
            .Where(x => x.SourceProvider == selectedProvider && seriesExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

        var syncedAtUtc = DateTimeOffset.UtcNow;
        var seriesUpserted = 0;
        var seriesMatchesUpserted = 0;

        foreach (var providerSeries in selectedSeries)
        {
            if (!existingSeries.TryGetValue(providerSeries.ExternalId, out var series))
            {
                series = new Series
                {
                    Id = Guid.NewGuid(),
                    SourceProvider = selectedProvider,
                    ExternalId = providerSeries.ExternalId
                };

                _dbContext.Series.Add(series);
                existingSeries[providerSeries.ExternalId] = series;
            }

            var details = await providerUsed.GetSeriesInfoAsync(providerSeries.ExternalId, cancellationToken);

            series.Name = string.IsNullOrWhiteSpace(details?.Name) ? providerSeries.Name : details.Name;
            series.StartDateUtc = details?.StartDateUtc ?? providerSeries.StartDateUtc;
            series.EndDateUtc = details?.EndDateUtc ?? providerSeries.EndDateUtc;
            series.LastSyncedAtUtc = syncedAtUtc;
            seriesUpserted++;

            if (details?.Matches is null || details.Matches.Count == 0)
            {
                continue;
            }

            var detailsMatchIds = details.Matches
                .Select(x => x.ExternalId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingSeriesMatches = (await _dbContext.SeriesMatches
                .Where(x => x.SeriesId == series.Id
                    && x.SourceProvider == selectedProvider
                    && detailsMatchIds.Contains(x.ExternalId))
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

            foreach (var providerMatch in details.Matches)
            {
                if (!existingSeriesMatches.TryGetValue(providerMatch.ExternalId, out var seriesMatch))
                {
                    seriesMatch = new SeriesMatch
                    {
                        Id = Guid.NewGuid(),
                        SeriesId = series.Id,
                        SourceProvider = selectedProvider,
                        ExternalId = providerMatch.ExternalId
                    };

                    _dbContext.SeriesMatches.Add(seriesMatch);
                    existingSeriesMatches[providerMatch.ExternalId] = seriesMatch;
                }

                seriesMatch.Name = providerMatch.Name;
                seriesMatch.Format = providerMatch.Format;
                seriesMatch.StartTimeUtc = providerMatch.StartTimeUtc;
                seriesMatch.Status = providerMatch.Status;
                seriesMatch.StatusText = providerMatch.StatusText;
                seriesMatch.LastSyncedAtUtc = syncedAtUtc;
                seriesMatchesUpserted++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SeriesSyncResult(
            ProviderUsed: selectedProvider,
            ProvidersTried: providersTried,
            SeriesUpserted: seriesUpserted,
            SeriesMatchesUpserted: seriesMatchesUpserted,
            SyncedAtUtc: syncedAtUtc);
    }

    private List<string> BuildProviderPriority()
    {
        var priority = new List<string>();

        foreach (var providerName in _options.Priority)
        {
            if (!string.IsNullOrWhiteSpace(providerName) &&
                !priority.Contains(providerName, StringComparer.OrdinalIgnoreCase))
            {
                priority.Add(providerName.Trim());
            }
        }

        foreach (var providerName in _providers.Keys)
        {
            if (!priority.Contains(providerName, StringComparer.OrdinalIgnoreCase))
            {
                priority.Add(providerName);
            }
        }

        return priority;
    }

    private static bool IsTestProviderName(string providerName)
    {
        return providerName.StartsWith("Test", StringComparison.OrdinalIgnoreCase)
            || providerName.StartsWith("Fixture", StringComparison.OrdinalIgnoreCase);
    }
}
