using CricStats.Application.Interfaces;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Entities;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure.Services;

public sealed class UpcomingMatchesSyncService : IUpcomingMatchesSyncService
{
    private readonly CricStatsDbContext _dbContext;
    private readonly IReadOnlyDictionary<string, ICricketProvider> _providers;
    private readonly CricketProvidersOptions _options;
    private readonly ILogger<UpcomingMatchesSyncService> _logger;
    private readonly bool _isTestingEnvironment;

    public UpcomingMatchesSyncService(
        CricStatsDbContext dbContext,
        IEnumerable<ICricketProvider> providers,
        IOptions<CricketProvidersOptions> options,
        ILogger<UpcomingMatchesSyncService> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _dbContext = dbContext;
        _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _logger = logger;
        _isTestingEnvironment = hostEnvironment?.IsEnvironment("Testing") ?? true;
    }

    public async Task<UpcomingMatchesSyncResult> SyncUpcomingMatchesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var fromUtc = now.AddHours(-2);
        var toUtc = now.AddDays(Math.Clamp(_options.SyncWindowDays, 1, 30));

        var providerOrder = BuildProviderPriority();
        var providersTried = new List<string>();

        IReadOnlyList<ProviderUpcomingMatch> selectedFixtures = [];
        string? selectedProvider = null;

        foreach (var providerName in providerOrder)
        {
            if (!_isTestingEnvironment && IsTestProviderName(providerName))
            {
                _logger.LogInformation(
                    "Skipping provider '{ProviderName}' because test providers are disabled outside Testing.",
                    providerName);
                continue;
            }

            providersTried.Add(providerName);

            if (!_providers.TryGetValue(providerName, out var provider))
            {
                _logger.LogWarning("Configured provider '{ProviderName}' was not registered.", providerName);
                continue;
            }

            try
            {
                var upcomingMatches = await provider.GetUpcomingMatchesAsync(fromUtc, toUtc, cancellationToken);
                if (upcomingMatches.Count == 0)
                {
                    _logger.LogInformation("Provider '{ProviderName}' returned no fixtures.", providerName);
                    continue;
                }

                selectedFixtures = upcomingMatches;
                selectedProvider = provider.Name;
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{ProviderName}' failed during fixture sync.", providerName);
            }
        }

        if (selectedProvider is null)
        {
            return new UpcomingMatchesSyncResult(
                ProviderUsed: null,
                ProvidersTried: providersTried,
                MatchesInserted: 0,
                MatchesUpdated: 0,
                TeamsUpserted: 0,
                VenuesUpserted: 0,
                SyncedAtUtc: now);
        }

        var matchExternalIds = selectedFixtures.Select(x => x.ExternalId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var venueExternalIds = selectedFixtures.Select(x => x.Venue.ExternalId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var teamExternalIds = selectedFixtures
            .SelectMany(x => new[] { x.HomeTeam.ExternalId, x.AwayTeam.ExternalId })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingMatches = (await _dbContext.Matches
            .Where(x => x.SourceProvider == selectedProvider && matchExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

        var existingVenues = (await _dbContext.Venues
            .Where(x => x.SourceProvider == selectedProvider && venueExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

        var existingTeams = (await _dbContext.Teams
            .Where(x => x.SourceProvider == selectedProvider && teamExternalIds.Contains(x.ExternalId))
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.ExternalId, StringComparer.OrdinalIgnoreCase);

        var syncedAtUtc = DateTimeOffset.UtcNow;
        var matchesInserted = 0;
        var matchesUpdated = 0;

        foreach (var fixture in selectedFixtures)
        {
            var venue = UpsertVenue(selectedProvider, fixture.Venue, syncedAtUtc, existingVenues);
            var homeTeam = UpsertTeam(selectedProvider, fixture.HomeTeam, syncedAtUtc, existingTeams);
            var awayTeam = UpsertTeam(selectedProvider, fixture.AwayTeam, syncedAtUtc, existingTeams);

            if (!existingMatches.TryGetValue(fixture.ExternalId, out var match))
            {
                match = new Match
                {
                    Id = Guid.NewGuid(),
                    SourceProvider = selectedProvider,
                    ExternalId = fixture.ExternalId
                };

                _dbContext.Matches.Add(match);
                existingMatches[fixture.ExternalId] = match;
                matchesInserted++;
            }
            else
            {
                matchesUpdated++;
            }

            match.Format = fixture.Format;
            match.StartTimeUtc = fixture.StartTimeUtc;
            match.Status = fixture.Status;
            match.VenueId = venue.Id;
            match.HomeTeamId = homeTeam.Id;
            match.AwayTeamId = awayTeam.Id;
            match.LastSyncedAtUtc = syncedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpcomingMatchesSyncResult(
            ProviderUsed: selectedProvider,
            ProvidersTried: providersTried,
            MatchesInserted: matchesInserted,
            MatchesUpdated: matchesUpdated,
            TeamsUpserted: teamExternalIds.Count,
            VenuesUpserted: venueExternalIds.Count,
            SyncedAtUtc: syncedAtUtc);
    }

    private static bool IsTestProviderName(string providerName)
    {
        return providerName.StartsWith("Test", StringComparison.OrdinalIgnoreCase)
            || providerName.StartsWith("Fixture", StringComparison.OrdinalIgnoreCase);
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

    private Venue UpsertVenue(
        string sourceProvider,
        ProviderVenue providerVenue,
        DateTimeOffset syncedAtUtc,
        IDictionary<string, Venue> existingVenues)
    {
        if (!existingVenues.TryGetValue(providerVenue.ExternalId, out var venue))
        {
            venue = new Venue
            {
                Id = Guid.NewGuid(),
                SourceProvider = sourceProvider,
                ExternalId = providerVenue.ExternalId
            };

            _dbContext.Venues.Add(venue);
            existingVenues[providerVenue.ExternalId] = venue;
        }

        venue.Name = providerVenue.Name;
        venue.City = providerVenue.City;
        venue.Country = providerVenue.Country;
        venue.Latitude = providerVenue.Latitude;
        venue.Longitude = providerVenue.Longitude;
        venue.LastSyncedAtUtc = syncedAtUtc;

        return venue;
    }

    private Team UpsertTeam(
        string sourceProvider,
        ProviderTeam providerTeam,
        DateTimeOffset syncedAtUtc,
        IDictionary<string, Team> existingTeams)
    {
        if (!existingTeams.TryGetValue(providerTeam.ExternalId, out var team))
        {
            team = new Team
            {
                Id = Guid.NewGuid(),
                SourceProvider = sourceProvider,
                ExternalId = providerTeam.ExternalId
            };

            _dbContext.Teams.Add(team);
            existingTeams[providerTeam.ExternalId] = team;
        }

        team.Name = providerTeam.Name;
        team.Country = providerTeam.Country;
        team.ShortName = providerTeam.ShortName;
        team.LastSyncedAtUtc = syncedAtUtc;

        return team;
    }
}
