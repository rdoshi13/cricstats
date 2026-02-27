using CricStats.Application.Interfaces;
using CricStats.Contracts.Series;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/series")]
public sealed class SeriesController : ControllerBase
{
    private readonly IUpcomingSeriesService _upcomingSeriesService;
    private readonly ISeriesDetailsService _seriesDetailsService;

    public SeriesController(
        IUpcomingSeriesService upcomingSeriesService,
        ISeriesDetailsService seriesDetailsService)
    {
        _upcomingSeriesService = upcomingSeriesService;
        _seriesDetailsService = seriesDetailsService;
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SeriesDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesDetailsResponse>> GetSeriesById(
        Guid id,
        [FromQuery] GetSeriesByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["page"] = ["'page' must be greater than or equal to 1."]
            }));
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["pageSize"] = ["'pageSize' must be between 1 and 100."]
            }));
        }

        var response = await _seriesDetailsService.GetSeriesByIdAsync(
            id,
            query.Page,
            query.PageSize,
            cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
}
