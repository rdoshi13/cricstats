using CricStats.Application.Interfaces;
using CricStats.Contracts.Admin;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/admin/sync")]
public sealed class AdminSyncController : ControllerBase
{
    private readonly IUpcomingMatchesSyncService _upcomingMatchesSyncService;
    private readonly ISeriesSyncService _seriesSyncService;

    public AdminSyncController(
        IUpcomingMatchesSyncService upcomingMatchesSyncService,
        ISeriesSyncService seriesSyncService)
    {
        _upcomingMatchesSyncService = upcomingMatchesSyncService;
        _seriesSyncService = seriesSyncService;
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

    [HttpPost("series")]
    [ProducesResponseType(typeof(SyncUpcomingSeriesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SyncUpcomingSeriesResponse>> SyncUpcomingSeries(
        CancellationToken cancellationToken)
    {
        var result = await _seriesSyncService.SyncUpcomingSeriesAsync(cancellationToken);

        return Ok(new SyncUpcomingSeriesResponse(
            result.ProviderUsed,
            result.ProvidersTried,
            result.SeriesUpserted,
            result.SeriesMatchesUpserted,
            result.SyncedAtUtc));
    }
}
