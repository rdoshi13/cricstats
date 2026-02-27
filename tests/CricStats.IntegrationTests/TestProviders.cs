using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Application.Models.Weather;
using CricStats.Domain.Enums;

namespace CricStats.IntegrationTests;

// Deterministic fixtures used by integration tests only.
internal sealed class FixtureCricketProvider : ICricketProvider
{
    public string Name => "FixtureCricketProvider";

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
                ExternalId: "fixture-match-ind-aus-t20",
                Format: MatchFormat.T20,
                StartTimeUtc: baseDay.AddDays(1).AddHours(14),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue("fixture-venue-wankhede", "Wankhede Stadium", "Mumbai", "India", 18.9389m, 72.8258m),
                HomeTeam: new ProviderTeam("fixture-team-india", "India", "India", "IND"),
                AwayTeam: new ProviderTeam("fixture-team-australia", "Australia", "Australia", "AUS")),
            new(
                ExternalId: "fixture-match-wi-eng-odi",
                Format: MatchFormat.ODI,
                StartTimeUtc: baseDay.AddDays(3).AddHours(9).AddMinutes(30),
                Status: MatchStatus.Scheduled,
                Venue: new ProviderVenue("fixture-venue-kensington", "Kensington Oval", "Bridgetown", "West Indies", 13.1045m, -59.6133m),
                HomeTeam: new ProviderTeam("fixture-team-west-indies", "West Indies", "West Indies", "WI"),
                AwayTeam: new ProviderTeam("fixture-team-england", "England", "England", "ENG"))
        };

        var filtered = matches
            .Where(x => x.StartTimeUtc >= fromUtc && x.StartTimeUtc <= toUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
    }

    public Task<IReadOnlyList<ProviderSeries>> GetUpcomingSeriesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var baseDay = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);
        var series = new List<ProviderSeries>
        {
            new(
                ExternalId: "fixture-series-asia-cup",
                Name: "Asia Cup",
                StartDateUtc: baseDay.AddDays(2),
                EndDateUtc: baseDay.AddDays(20))
        };

        var filtered = series
            .Where(x => (x.StartDateUtc ?? x.EndDateUtc ?? baseDay) >= fromUtc)
            .Where(x => (x.StartDateUtc ?? x.EndDateUtc ?? baseDay) <= toUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProviderSeries>>(filtered);
    }

    public Task<ProviderSeriesDetails?> GetSeriesInfoAsync(
        string seriesExternalId,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(seriesExternalId, "fixture-series-asia-cup", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<ProviderSeriesDetails?>(null);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var baseDay = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);

        var details = new ProviderSeriesDetails(
            ExternalId: "fixture-series-asia-cup",
            Name: "Asia Cup",
            StartDateUtc: baseDay.AddDays(2),
            EndDateUtc: baseDay.AddDays(20),
            Matches:
            [
                new ProviderSeriesMatch(
                    ExternalId: "fixture-series-match-1",
                    Name: "India vs Pakistan",
                    Format: MatchFormat.ODI,
                    StartTimeUtc: baseDay.AddDays(3).AddHours(10),
                    Status: MatchStatus.Scheduled,
                    StatusText: "Scheduled"),
                new ProviderSeriesMatch(
                    ExternalId: "fixture-series-match-2",
                    Name: "Sri Lanka vs Bangladesh",
                    Format: MatchFormat.ODI,
                    StartTimeUtc: baseDay.AddDays(4).AddHours(10),
                    Status: MatchStatus.Scheduled,
                    StatusText: "Scheduled")
            ]);

        return Task.FromResult<ProviderSeriesDetails?>(details);
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
