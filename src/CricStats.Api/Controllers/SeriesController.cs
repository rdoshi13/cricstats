using CricStats.Application.Interfaces;
using CricStats.Contracts.Series;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/series")]
public sealed class SeriesController : ControllerBase
{
    private readonly IUpcomingSeriesService _upcomingSeriesService;

    public SeriesController(IUpcomingSeriesService upcomingSeriesService)
    {
        _upcomingSeriesService = upcomingSeriesService;
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(UpcomingSeriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpcomingSeriesResponse>> GetUpcomingSeries(
        [FromQuery] GetUpcomingSeriesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["from"] = ["'from' must be less than or equal to 'to'."]
            };

            return BadRequest(new ValidationProblemDetails(errors));
        }

        var response = await _upcomingSeriesService.GetUpcomingSeriesAsync(
            query.From,
            query.To,
            cancellationToken);

        return Ok(response);
    }
}
