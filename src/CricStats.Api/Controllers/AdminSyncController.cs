using CricStats.Application.Interfaces;
using CricStats.Contracts.Admin;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/admin/sync")]
public sealed class AdminSyncController : ControllerBase
{
    private readonly IUpcomingMatchesSyncService _upcomingMatchesSyncService;

    public AdminSyncController(IUpcomingMatchesSyncService upcomingMatchesSyncService)
    {
        _upcomingMatchesSyncService = upcomingMatchesSyncService;
    }

    [HttpPost("upcoming")]
    [ProducesResponseType(typeof(SyncUpcomingMatchesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SyncUpcomingMatchesResponse>> SyncUpcomingMatches(
        CancellationToken cancellationToken)
    {
        var result = await _upcomingMatchesSyncService.SyncUpcomingMatchesAsync(cancellationToken);

        return Ok(new SyncUpcomingMatchesResponse(
            result.ProviderUsed,
            result.ProvidersTried,
            result.MatchesInserted,
            result.MatchesUpdated,
            result.TeamsUpserted,
            result.VenuesUpserted,
            result.SyncedAtUtc));
    }
}
