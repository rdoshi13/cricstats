using CricStats.Application.Models;

namespace CricStats.Application.Interfaces;

public interface IUpcomingMatchesSyncService
{
    Task<UpcomingMatchesSyncResult> SyncUpcomingMatchesAsync(
        CancellationToken cancellationToken = default);
}
