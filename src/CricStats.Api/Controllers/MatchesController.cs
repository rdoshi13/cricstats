using CricStats.Application.Interfaces;
using CricStats.Application.Models;
using CricStats.Contracts.Matches;
using CricStats.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CricStats.Api.Controllers;

[ApiController]
[Route("api/v1/matches")]
public sealed class MatchesController : ControllerBase
{
    private static readonly string[] ValidFormats = Enum.GetNames<MatchFormat>();
    private readonly IUpcomingMatchesService _upcomingMatchesService;

    public MatchesController(IUpcomingMatchesService upcomingMatchesService)
    {
        _upcomingMatchesService = upcomingMatchesService;
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(UpcomingMatchesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpcomingMatchesResponse>> GetUpcomingMatches(
        [FromQuery] GetUpcomingMatchesQuery query,
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

        MatchFormat? parsedFormat = null;
        if (!string.IsNullOrWhiteSpace(query.Format))
        {
            if (!Enum.TryParse<MatchFormat>(query.Format, ignoreCase: true, out var format))
            {
                var errors = new Dictionary<string, string[]>
                {
                    ["format"] = [$"Invalid format. Allowed values: {string.Join(", ", ValidFormats)}."]
                };

                return BadRequest(new ValidationProblemDetails(errors));
            }

            parsedFormat = format;
        }

        var filter = new UpcomingMatchesFilter(query.Country, parsedFormat, query.From, query.To);
        var response = await _upcomingMatchesService.GetUpcomingMatchesAsync(filter, cancellationToken);

        return Ok(response);
    }
}
