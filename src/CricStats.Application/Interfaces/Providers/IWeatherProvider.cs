using CricStats.Application.Models.Weather;

namespace CricStats.Application.Interfaces.Providers;

public interface IWeatherProvider
{
    string Name { get; }

    Task<IReadOnlyList<WeatherForecastPoint>> GetForecastAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
