using CricStats.Application.Interfaces;
using CricStats.Contracts.Series;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CricStats.Infrastructure.Services;

public sealed class SeriesDetailsService : ISeriesDetailsService
{
    private readonly CricStatsDbContext _dbContext;

    public SeriesDetailsService(CricStatsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SeriesDetailsResponse?> GetSeriesByIdAsync(
        Guid seriesId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var series = await _dbContext.Series
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == seriesId, cancellationToken);

        if (series is null)
        {
            return null;
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var skip = (safePage - 1) * safePageSize;

        var matchesQuery = _dbContext.SeriesMatches
            .AsNoTracking()
            .Where(x => x.SeriesId == series.Id)
            .OrderBy(x => x.StartTimeUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.Name);

        var totalMatchCount = await matchesQuery.CountAsync(cancellationToken);
        var rawMatches = await matchesQuery
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var matches = rawMatches
            .Select(m => new SeriesUpcomingMatchItem(
                m.Id,
                m.ExternalId,
                m.Name,
                m.VenueName ?? "Unknown Venue",
                m.VenueCountry ?? "Unknown",
                m.Format?.ToString(),
                m.StartTimeUtc,
                m.Status?.ToString(),
                m.StatusText))
            .ToList();

        return new SeriesDetailsResponse(
            series.Id,
            series.ExternalId,
            series.Name,
            series.StartDateUtc,
            series.EndDateUtc,
            series.SourceProvider,
            safePage,
            safePageSize,
            totalMatchCount,
            matches);
    }
}
