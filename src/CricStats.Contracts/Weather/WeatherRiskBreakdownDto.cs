namespace CricStats.Contracts.Weather;

public sealed record WeatherRiskBreakdownDto(
    decimal AveragePrecipProbability,
    decimal AveragePrecipAmount,
    decimal AverageHumidity,
    decimal AverageWindSpeed,
    decimal PrecipProbabilityContribution,
    decimal PrecipAmountContribution,
    decimal HumidityContribution,
    decimal WindSpeedContribution);
