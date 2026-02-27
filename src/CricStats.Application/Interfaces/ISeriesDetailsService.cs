using CricStats.Contracts.Series;

namespace CricStats.Application.Interfaces;

public interface ISeriesDetailsService
{
    Task<SeriesDetailsResponse?> GetSeriesByIdAsync(
        Guid seriesId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
