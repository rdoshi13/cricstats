using CricStats.Application.Interfaces;
using CricStats.Application.Models;
using CricStats.Contracts.Matches;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CricStats.Infrastructure.Services;

public sealed class UpcomingMatchesService : IUpcomingMatchesService
{
    private readonly CricStatsDbContext _dbContext;
    private readonly IUpcomingMatchesSyncService _syncService;
    private readonly ILogger<UpcomingMatchesService> _logger;

    public UpcomingMatchesService(
        CricStatsDbContext dbContext,
        IUpcomingMatchesSyncService syncService,
        ILogger<UpcomingMatchesService> logger)
    {
        _dbContext = dbContext;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task<UpcomingMatchesResponse> GetUpcomingMatchesAsync(
        UpcomingMatchesFilter filter,
        CancellationToken cancellationToken = default)
    {
        var matches = await QueryMatchesAsync(filter, cancellationToken);

        if (matches.Count == 0 && !await _dbContext.Matches.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("No upcoming matches in database. Triggering provider sync.");
            await _syncService.SyncUpcomingMatchesAsync(cancellationToken);
            matches = await QueryMatchesAsync(filter, cancellationToken);
        }

        return new UpcomingMatchesResponse(matches, matches.Count);
    }

    private async Task<List<UpcomingMatchItem>> QueryMatchesAsync(
        UpcomingMatchesFilter filter,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _dbContext.Matches
            .AsNoTracking()
            .Include(x => x.Venue)
            .Include(x => x.HomeTeam)
            .Include(x => x.AwayTeam)
            .Include(x => x.MatchWeatherRisk)
            .Where(x => x.StartTimeUtc >= (filter.From ?? now));

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            var countryFilter = filter.Country.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Venue.Country.ToLower() == countryFilter ||
                x.HomeTeam.Country.ToLower() == countryFilter ||
                x.AwayTeam.Country.ToLower() == countryFilter);
        }

        if (filter.Format is not null)
        {
            query = query.Where(x => x.Format == filter.Format.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(x => x.StartTimeUtc <= filter.To.Value);
        }

        var matches = await query
            .OrderBy(x => x.StartTimeUtc)
            .ToListAsync(cancellationToken);

        return matches
            .Select(x => new UpcomingMatchItem(
                x.Id,
                x.Format.ToString(),
                x.StartTimeUtc,
                x.VenueId,
                x.Venue.Name,
                x.Venue.Country,
                x.HomeTeamId,
                x.HomeTeam.Name,
                x.HomeTeam.Country,
                x.AwayTeamId,
                x.AwayTeam.Name,
                x.AwayTeam.Country,
                x.Status.ToString(),
                x.MatchWeatherRisk is null
                    ? new WeatherRiskSummaryDto(0m, RiskLevel.Low.ToString())
                    : new WeatherRiskSummaryDto(
                        x.MatchWeatherRisk.CompositeRiskScore,
                        x.MatchWeatherRisk.RiskLevel.ToString())))
            .ToList();
    }
}
