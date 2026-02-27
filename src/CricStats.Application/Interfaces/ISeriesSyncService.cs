using CricStats.Application.Models;

namespace CricStats.Application.Interfaces;

public interface ISeriesSyncService
{
    Task<SeriesSyncResult> SyncUpcomingSeriesAsync(
        CancellationToken cancellationToken = default);
}
