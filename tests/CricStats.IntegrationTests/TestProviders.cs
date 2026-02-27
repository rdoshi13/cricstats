using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Application.Models.Weather;
using CricStats.Domain.Enums;

namespace CricStats.IntegrationTests;

internal sealed class TestCricketProvider : ICricketProvider
{
    public string Name => "TestCricket";

    public Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var baseDay = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);

        var matches = new List<ProviderUpcomingMatch>
        {
            new(
                ExternalId: "test-match-001",
                Format: MatchFormat.T20,
                StartTimeUtc: baseDay.AddDays(1).AddHours(14),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue("test-venue-001", "Wankhede Stadium", "Mumbai", "India", 18.9389m, 72.8258m),
                HomeTeam: new ProviderTeam("test-team-001", "India", "India", "IND"),
                AwayTeam: new ProviderTeam("test-team-002", "Australia", "Australia", "AUS")),
            new(
                ExternalId: "test-match-002",
                Format: MatchFormat.ODI,
                StartTimeUtc: baseDay.AddDays(3).AddHours(9).AddMinutes(30),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue("test-venue-002", "Kensington Oval", "Bridgetown", "West Indies", 13.1045m, -59.6133m),
                HomeTeam: new ProviderTeam("test-team-003", "West Indies", "West Indies", "WI"),
                AwayTeam: new ProviderTeam("test-team-004", "England", "England", "ENG"))
        };

        var filtered = matches
            .Where(x => x.StartTimeUtc >= fromUtc && x.StartTimeUtc <= toUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
    }
}

internal sealed class TestWeatherProvider : IWeatherProvider
{
    public string Name => "OpenMeteoStub";

    public Task<IReadOnlyList<WeatherForecastPoint>> GetForecastAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var current = new DateTimeOffset(fromUtc.UtcDateTime.Year, fromUtc.UtcDateTime.Month, fromUtc.UtcDateTime.Day, fromUtc.UtcDateTime.Hour, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(toUtc.UtcDateTime.Year, toUtc.UtcDateTime.Month, toUtc.UtcDateTime.Day, toUtc.UtcDateTime.Hour, 0, 0, TimeSpan.Zero);

        var points = new List<WeatherForecastPoint>();
        while (current <= end)
        {
            points.Add(new WeatherForecastPoint(
                ExternalId: $"test-weather-{current:yyyyMMddHH}",
                TimestampUtc: current,
                Temperature: 26m,
                Humidity: 62m,
                WindSpeed: 14m,
                PrecipProbability: 28m,
                PrecipAmount: 1.4m));

            current = current.AddHours(1);
        }

        return Task.FromResult<IReadOnlyList<WeatherForecastPoint>>(points);
    }
}
