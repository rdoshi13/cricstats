using CricStats.Application.Interfaces;
using CricStats.Contracts.Series;
using CricStats.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CricStats.Infrastructure.Services;

public sealed class UpcomingSeriesService : IUpcomingSeriesService
{
    private readonly CricStatsDbContext _dbContext;
    private readonly ISeriesSyncService _seriesSyncService;
    private readonly ILogger<UpcomingSeriesService> _logger;
    private readonly bool _isTestingEnvironment;

    public UpcomingSeriesService(
        CricStatsDbContext dbContext,
        ISeriesSyncService seriesSyncService,
        ILogger<UpcomingSeriesService> logger,
        IHostEnvironment? hostEnvironment = null)
    {
        _dbContext = dbContext;
        _seriesSyncService = seriesSyncService;
        _logger = logger;
        _isTestingEnvironment = hostEnvironment?.IsEnvironment("Testing") ?? true;
    }

    public async Task<UpcomingSeriesResponse> GetUpcomingSeriesAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
    {
        var series = await QuerySeriesAsync(fromUtc, toUtc, cancellationToken);
        if (series.Count == 0 && !await _dbContext.Series.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("No series in database. Triggering series sync.");
            await _seriesSyncService.SyncUpcomingSeriesAsync(cancellationToken);
            series = await QuerySeriesAsync(fromUtc, toUtc, cancellationToken);
        }

        return new UpcomingSeriesResponse(series, series.Count);
    }

    private async Task<List<UpcomingSeriesItem>> QuerySeriesAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var from = fromUtc ?? DateTimeOffset.UtcNow;

        var query = _dbContext.Series
            .AsNoTracking()
            .Include(x => x.Matches)
            .Where(x => (x.StartDateUtc ?? x.EndDateUtc ?? x.LastSyncedAtUtc) >= from);

        if (!_isTestingEnvironment)
        {
            query = query.Where(x =>
                !x.SourceProvider.StartsWith("Test") &&
                !x.SourceProvider.StartsWith("Fixture"));
        }

        if (toUtc is not null)
        {
            query = query.Where(x => (x.StartDateUtc ?? x.EndDateUtc ?? x.LastSyncedAtUtc) <= toUtc.Value);
        }

        var series = await query
            .OrderBy(x => x.StartDateUtc ?? x.EndDateUtc ?? x.LastSyncedAtUtc)
            .ToListAsync(cancellationToken);

        return series.Select(x => new UpcomingSeriesItem(
                x.Id,
                x.ExternalId,
                x.Name,
                x.StartDateUtc,
                x.EndDateUtc,
                x.SourceProvider,
                x.Matches
                    .Where(m => m.StartTimeUtc is null || m.StartTimeUtc >= from)
                    .OrderBy(m => m.StartTimeUtc ?? DateTimeOffset.MaxValue)
                    .Take(10)
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
                    .ToList()))
            .ToList();
    }
}
