using CricStats.Application.Interfaces;
using CricStats.Application.Models;
using CricStats.Contracts.Matches;
using CricStats.Domain.Enums;

namespace CricStats.Application.Services;

public sealed class UpcomingMatchesService : IUpcomingMatchesService
{
    private static readonly IReadOnlyList<StubUpcomingMatch> StubMatches =
    [
        new StubUpcomingMatch(
            Guid.Parse("0f75bb15-e6ae-4228-a026-32fbc87d6dd1"),
            MatchFormat.T20,
            new DateTimeOffset(2026, 3, 3, 14, 0, 0, TimeSpan.Zero),
            Guid.Parse("d93f417d-66f5-4056-8e79-896f5e9fb46f"),
            "Wankhede Stadium",
            "India",
            Guid.Parse("aebfaace-331b-4fd9-9549-d638f0c77145"),
            "India",
            "India",
            Guid.Parse("ccf4f354-c598-4197-b71d-4e30e2054897"),
            "Australia",
            "Australia",
            MatchStatus.Scheduled,
            71.4m,
            RiskLevel.High),
        new StubUpcomingMatch(
            Guid.Parse("4a383a7f-9f0e-4f48-9724-5ab13e259fd3"),
            MatchFormat.ODI,
            new DateTimeOffset(2026, 3, 5, 9, 30, 0, TimeSpan.Zero),
            Guid.Parse("5607303a-f33f-4886-8f5e-83d67e3e1a5e"),
            "Kensington Oval",
            "West Indies",
            Guid.Parse("ef826330-f5d9-4279-8944-665ba5276f1d"),
            "West Indies",
            "West Indies",
            Guid.Parse("b5a5fc95-a26f-42f1-bd90-b66866ac8d65"),
            "England",
            "England",
            MatchStatus.Scheduled,
            38.2m,
            RiskLevel.Medium),
        new StubUpcomingMatch(
            Guid.Parse("5eac6f53-c6e1-4c7f-a328-0bec1dde0f3d"),
            MatchFormat.Test,
            new DateTimeOffset(2026, 3, 8, 4, 0, 0, TimeSpan.Zero),
            Guid.Parse("f69d3d69-cb32-4f95-99af-e98f5cfd75ad"),
            "Lord's",
            "England",
            Guid.Parse("577f05a1-a4cf-4aa2-9401-835a85110dcb"),
            "England",
            "England",
            Guid.Parse("735ec0a2-65df-4d5c-9d6f-9d3e0ee9ccf5"),
            "South Africa",
            "South Africa",
            MatchStatus.Scheduled,
            24.7m,
            RiskLevel.Low)
    ];

    public Task<UpcomingMatchesResponse> GetUpcomingMatchesAsync(
        UpcomingMatchesFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<StubUpcomingMatch> query = StubMatches;

        // Country filter matches venue country OR either team's country.
        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            query = query.Where(x =>
                IsMatch(x.VenueCountry, filter.Country) ||
                IsMatch(x.HomeTeamCountry, filter.Country) ||
                IsMatch(x.AwayTeamCountry, filter.Country));
        }

        if (filter.Format is not null)
        {
            query = query.Where(x => x.Format == filter.Format.Value);
        }

        if (filter.From is not null)
        {
            query = query.Where(x => x.StartTimeUtc >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(x => x.StartTimeUtc <= filter.To.Value);
        }

        var results = query
            .OrderBy(x => x.StartTimeUtc)
            .Select(x => new UpcomingMatchItem(
                x.MatchId,
                x.Format.ToString(),
                x.StartTimeUtc,
                x.VenueId,
                x.VenueName,
                x.VenueCountry,
                x.HomeTeamId,
                x.HomeTeamName,
                x.HomeTeamCountry,
                x.AwayTeamId,
                x.AwayTeamName,
                x.AwayTeamCountry,
                x.Status.ToString(),
                new WeatherRiskSummaryDto(x.CompositeRiskScore, x.RiskLevel.ToString())))
            .ToList();

        return Task.FromResult(new UpcomingMatchesResponse(results, results.Count));
    }

    private static bool IsMatch(string actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StubUpcomingMatch(
        Guid MatchId,
        MatchFormat Format,
        DateTimeOffset StartTimeUtc,
        Guid VenueId,
        string VenueName,
        string VenueCountry,
        Guid HomeTeamId,
        string HomeTeamName,
        string HomeTeamCountry,
        Guid AwayTeamId,
        string AwayTeamName,
        string AwayTeamCountry,
        MatchStatus Status,
        decimal CompositeRiskScore,
        RiskLevel RiskLevel);
}
