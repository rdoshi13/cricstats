using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Weather;
using Microsoft.Extensions.Logging;

namespace CricStats.Infrastructure.Providers.Weather;

public sealed class OpenMeteoStubProvider : IWeatherProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenMeteoStubProvider> _logger;

    public OpenMeteoStubProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<OpenMeteoStubProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "OpenMeteoStub";

    public async Task<IReadOnlyList<WeatherForecastPoint>> GetForecastAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var liveForecast = await TryGetLiveForecastAsync(latitude, longitude, fromUtc, toUtc, cancellationToken);
            if (liveForecast.Count > 0)
            {
                return liveForecast;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open-Meteo request failed.");
        }

        return [];
    }

    private async Task<IReadOnlyList<WeatherForecastPoint>> TryGetLiveForecastAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var startDate = fromUtc.ToUniversalTime().ToString("yyyy-MM-dd");
        var endDate = toUtc.ToUniversalTime().ToString("yyyy-MM-dd");

        var endpoint =
            $"/v1/forecast?latitude={latitude}&longitude={longitude}" +
            "&hourly=temperature_2m,relative_humidity_2m,precipitation_probability,precipitation,wind_speed_10m" +
            $"&start_date={startDate}&end_date={endDate}&timezone=UTC";

        var client = _httpClientFactory.CreateClient("OpenMeteoWeather");
        var payload = await client.GetFromJsonAsync<OpenMeteoForecastResponse>(endpoint, cancellationToken);

        if (payload?.Hourly is null || payload.Hourly.Time is null || payload.Hourly.Time.Count == 0)
        {
            return [];
        }

        var fromBoundary = TruncateToHourUtc(fromUtc);
        var toBoundary = TruncateToHourUtc(toUtc);

        var results = new List<WeatherForecastPoint>();
        for (var i = 0; i < payload.Hourly.Time.Count; i++)
        {
            if (!DateTimeOffset.TryParse(payload.Hourly.Time[i], out var timestamp))
            {
                continue;
            }

            var timestampUtc = timestamp.ToUniversalTime();
            if (timestampUtc < fromBoundary || timestampUtc > toBoundary)
            {
                continue;
            }

            var temperature = GetValue(payload.Hourly.Temperature2M, i);
            var humidity = GetValue(payload.Hourly.RelativeHumidity2M, i);
            var precipProbability = GetValue(payload.Hourly.PrecipitationProbability, i);
            var precipAmount = GetValue(payload.Hourly.Precipitation, i);
            var windSpeed = GetValue(payload.Hourly.WindSpeed10M, i);

            results.Add(new WeatherForecastPoint(
                ExternalId: $"openmeteo-live-{latitude:F3}-{longitude:F3}-{timestampUtc:yyyyMMddHH}",
                TimestampUtc: timestampUtc,
                Temperature: temperature,
                Humidity: humidity,
                WindSpeed: windSpeed,
                PrecipProbability: precipProbability,
                PrecipAmount: precipAmount));
        }

        return results;
    }

    private static decimal GetValue(List<decimal?>? values, int index)
    {
        if (values is null || index < 0 || index >= values.Count || values[index] is null)
        {
            return 0m;
        }

        return values[index]!.Value;
    }

    private static DateTimeOffset TruncateToHourUtc(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    private sealed class OpenMeteoForecastResponse
    {
        [JsonPropertyName("hourly")]
        public OpenMeteoHourly? Hourly { get; init; }
    }

    private sealed class OpenMeteoHourly
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; init; }

        [JsonPropertyName("temperature_2m")]
        public List<decimal?>? Temperature2M { get; init; }

        [JsonPropertyName("relative_humidity_2m")]
        public List<decimal?>? RelativeHumidity2M { get; init; }

        [JsonPropertyName("precipitation_probability")]
        public List<decimal?>? PrecipitationProbability { get; init; }

        [JsonPropertyName("precipitation")]
        public List<decimal?>? Precipitation { get; init; }

        [JsonPropertyName("wind_speed_10m")]
        public List<decimal?>? WindSpeed10M { get; init; }
    }
}
