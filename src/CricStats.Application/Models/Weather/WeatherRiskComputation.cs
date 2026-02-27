using CricStats.Domain.Enums;

namespace CricStats.Application.Models.Weather;

public sealed record WeatherRiskComputation(
    decimal CompositeRiskScore,
    RiskLevel RiskLevel,
    decimal AveragePrecipProbability,
    decimal AveragePrecipAmount,
    decimal AverageHumidity,
    decimal AverageWindSpeed,
    decimal PrecipProbabilityContribution,
    decimal PrecipAmountContribution,
    decimal HumidityContribution,
    decimal WindSpeedContribution);
