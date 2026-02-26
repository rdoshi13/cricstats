using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Enums;

namespace CricStats.Infrastructure.Providers;

public sealed class ApiSportsProvider : ICricketProvider
{
    public string Name => "ApiSports";

    public Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = DateTimeOffset.UtcNow;
        var baseDay = new DateTimeOffset(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        var matches = new List<ProviderUpcomingMatch>
        {
            new(
                ExternalId: "apisports-match-2001",
                Format: MatchFormat.T20,
                StartTimeUtc: baseDay.AddDays(3).AddHours(13),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue(
                    ExternalId: "apisports-venue-401",
                    Name: "National Stadium Karachi",
                    City: "Karachi",
                    Country: "Pakistan",
                    Latitude: 24.8931m,
                    Longitude: 67.0817m),
                HomeTeam: new ProviderTeam(
                    ExternalId: "apisports-team-77",
                    Name: "Pakistan",
                    Country: "Pakistan",
                    ShortName: "PAK"),
                AwayTeam: new ProviderTeam(
                    ExternalId: "apisports-team-88",
                    Name: "New Zealand",
                    Country: "New Zealand",
                    ShortName: "NZ")),
            new(
                ExternalId: "apisports-match-2002",
                Format: MatchFormat.ODI,
                StartTimeUtc: baseDay.AddDays(5).AddHours(10),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue(
                    ExternalId: "apisports-venue-402",
                    Name: "R. Premadasa Stadium",
                    City: "Colombo",
                    Country: "Sri Lanka",
                    Latitude: 6.9271m,
                    Longitude: 79.8612m),
                HomeTeam: new ProviderTeam(
                    ExternalId: "apisports-team-99",
                    Name: "Sri Lanka",
                    Country: "Sri Lanka",
                    ShortName: "SL"),
                AwayTeam: new ProviderTeam(
                    ExternalId: "apisports-team-100",
                    Name: "Bangladesh",
                    Country: "Bangladesh",
                    ShortName: "BAN"))
        };

        var filtered = matches
            .Where(match => match.StartTimeUtc >= fromUtc && match.StartTimeUtc <= toUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
    }
}
