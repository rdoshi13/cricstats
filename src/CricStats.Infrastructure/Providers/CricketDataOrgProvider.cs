using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Enums;

namespace CricStats.Infrastructure.Providers;

public sealed class CricketDataOrgProvider : ICricketProvider
{
    public string Name => "CricketDataOrg";

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
                ExternalId: "cdorg-match-1001",
                Format: MatchFormat.T20,
                StartTimeUtc: baseDay.AddDays(2).AddHours(14),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue(
                    ExternalId: "cdorg-venue-301",
                    Name: "Wankhede Stadium",
                    City: "Mumbai",
                    Country: "India",
                    Latitude: 18.9389m,
                    Longitude: 72.8258m),
                HomeTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-11",
                    Name: "India",
                    Country: "India",
                    ShortName: "IND"),
                AwayTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-22",
                    Name: "Australia",
                    Country: "Australia",
                    ShortName: "AUS")),
            new(
                ExternalId: "cdorg-match-1002",
                Format: MatchFormat.ODI,
                StartTimeUtc: baseDay.AddDays(4).AddHours(9).AddMinutes(30),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue(
                    ExternalId: "cdorg-venue-302",
                    Name: "Kensington Oval",
                    City: "Bridgetown",
                    Country: "West Indies",
                    Latitude: 13.1045m,
                    Longitude: -59.6133m),
                HomeTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-33",
                    Name: "West Indies",
                    Country: "West Indies",
                    ShortName: "WI"),
                AwayTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-44",
                    Name: "England",
                    Country: "England",
                    ShortName: "ENG")),
            new(
                ExternalId: "cdorg-match-1003",
                Format: MatchFormat.Test,
                StartTimeUtc: baseDay.AddDays(7).AddHours(11),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue(
                    ExternalId: "cdorg-venue-303",
                    Name: "Lord's",
                    City: "London",
                    Country: "England",
                    Latitude: 51.5290m,
                    Longitude: -0.1722m),
                HomeTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-44",
                    Name: "England",
                    Country: "England",
                    ShortName: "ENG"),
                AwayTeam: new ProviderTeam(
                    ExternalId: "cdorg-team-55",
                    Name: "South Africa",
                    Country: "South Africa",
                    ShortName: "SA"))
        };

        var filtered = matches
            .Where(match => match.StartTimeUtc >= fromUtc && match.StartTimeUtc <= toUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
    }
}
