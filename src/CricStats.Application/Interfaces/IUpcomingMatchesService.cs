using CricStats.Application.Models;
using CricStats.Contracts.Matches;

namespace CricStats.Application.Interfaces;

public interface IUpcomingMatchesService
{
    Task<UpcomingMatchesResponse> GetUpcomingMatchesAsync(
        UpcomingMatchesFilter filter,
        CancellationToken cancellationToken = default);
}
