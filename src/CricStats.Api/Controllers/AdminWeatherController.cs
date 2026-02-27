using CricStats.Application.Interfaces;
using CricStats.Contracts.Admin;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/admin/weather")]
public sealed class AdminWeatherController : ControllerBase
{
    private readonly IWeatherRiskService _weatherRiskService;

    public AdminWeatherController(IWeatherRiskService weatherRiskService)
    {
        _weatherRiskService = weatherRiskService;
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshWeatherRiskResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RefreshWeatherRiskResponse>> RefreshWeatherRisk(
        CancellationToken cancellationToken)
    {
        var result = await _weatherRiskService.RefreshUpcomingWeatherRiskAsync(cancellationToken);

        return Ok(new RefreshWeatherRiskResponse(
            result.ProviderUsed,
            result.MatchesProcessed,
            result.RisksUpdated,
            result.RefreshedAtUtc));
    }
}
