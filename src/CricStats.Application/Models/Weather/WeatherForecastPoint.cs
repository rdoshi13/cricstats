namespace CricStats.Application.Models.Weather;

public sealed record WeatherForecastPoint(
    string ExternalId,
    DateTimeOffset TimestampUtc,
    decimal Temperature,
    decimal Humidity,
    decimal WindSpeed,
    decimal PrecipProbability,
    decimal PrecipAmount);
