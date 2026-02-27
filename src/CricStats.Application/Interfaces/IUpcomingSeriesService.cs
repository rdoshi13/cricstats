using CricStats.Contracts.Series;

namespace CricStats.Application.Interfaces;

public interface IUpcomingSeriesService
{
    Task<UpcomingSeriesResponse> GetUpcomingSeriesAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default);
}
