using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Weather;

namespace CricStats.Infrastructure.Providers.Weather;

public sealed class OpenMeteoStubProvider : IWeatherProvider
{
    public string Name => "OpenMeteoStub";

    public Task<IReadOnlyList<WeatherForecastPoint>> GetForecastAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var current = TruncateToHourUtc(fromUtc);
        var end = TruncateToHourUtc(toUtc);

        var forecasts = new List<WeatherForecastPoint>();
        while (current <= end)
        {
            var seed = Math.Abs(HashCode.Combine(
                decimal.ToInt32(decimal.Round(latitude * 1000m)),
                decimal.ToInt32(decimal.Round(longitude * 1000m)),
                current.DayOfYear,
                current.Hour));

            var precipProbability = seed % 101;
            var precipAmount = Math.Round((precipProbability / 100m) * ((seed % 12) + 1), 2);
            var humidity = 40 + (seed % 61);
            var windSpeed = 5 + (seed % 36);
            var temperature = 16 + (seed % 19);

            forecasts.Add(new WeatherForecastPoint(
                ExternalId: $"openmeteo-{latitude:F3}-{longitude:F3}-{current:yyyyMMddHH}",
                TimestampUtc: current,
                Temperature: temperature,
                Humidity: humidity,
                WindSpeed: windSpeed,
                PrecipProbability: precipProbability,
                PrecipAmount: precipAmount));

            current = current.AddHours(1);
        }

        return Task.FromResult<IReadOnlyList<WeatherForecastPoint>>(forecasts);
    }

    private static DateTimeOffset TruncateToHourUtc(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
