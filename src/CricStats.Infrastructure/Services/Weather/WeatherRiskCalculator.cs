using CricStats.Application.Models.Weather;
using CricStats.Domain.Enums;

namespace CricStats.Infrastructure.Services.Weather;

public static class WeatherRiskCalculator
{
    private const decimal PrecipProbabilityWeight = 0.5m;
    private const decimal PrecipAmountWeight = 0.3m;
    private const decimal HumidityWeight = 0.1m;
    private const decimal WindSpeedWeight = 0.1m;

    public static WeatherRiskComputation Compute(
        IReadOnlyList<WeatherForecastPoint> forecasts,
        decimal precipAmountMaxMm,
        decimal windSpeedMaxKph)
    {
        if (forecasts.Count == 0)
        {
            return new WeatherRiskComputation(0m, RiskLevel.Low, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m);
        }

        var avgPrecipProbability = forecasts.Average(x => x.PrecipProbability);
        var avgPrecipAmount = forecasts.Average(x => x.PrecipAmount);
        var avgHumidity = forecasts.Average(x => x.Humidity);
        var avgWindSpeed = forecasts.Average(x => x.WindSpeed);

        var normalizedPrecipAmount = Normalize(avgPrecipAmount, precipAmountMaxMm);
        var normalizedHumidity = Clamp(avgHumidity);
        var normalizedWindSpeed = Normalize(avgWindSpeed, windSpeedMaxKph);

        var precipProbabilityContribution = Math.Round(Clamp(avgPrecipProbability) * PrecipProbabilityWeight, 2);
        var precipAmountContribution = Math.Round(normalizedPrecipAmount * PrecipAmountWeight, 2);
        var humidityContribution = Math.Round(normalizedHumidity * HumidityWeight, 2);
        var windSpeedContribution = Math.Round(normalizedWindSpeed * WindSpeedWeight, 2);

        var compositeRiskScore = Math.Round(
            precipProbabilityContribution +
            precipAmountContribution +
            humidityContribution +
            windSpeedContribution,
            2);

        var riskLevel = compositeRiskScore switch
        {
            <= 33m => RiskLevel.Low,
            <= 66m => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        return new WeatherRiskComputation(
            CompositeRiskScore: compositeRiskScore,
            RiskLevel: riskLevel,
            AveragePrecipProbability: Math.Round(avgPrecipProbability, 2),
            AveragePrecipAmount: Math.Round(avgPrecipAmount, 2),
            AverageHumidity: Math.Round(avgHumidity, 2),
            AverageWindSpeed: Math.Round(avgWindSpeed, 2),
            PrecipProbabilityContribution: precipProbabilityContribution,
            PrecipAmountContribution: precipAmountContribution,
            HumidityContribution: humidityContribution,
            WindSpeedContribution: windSpeedContribution);
    }

    private static decimal Normalize(decimal value, decimal max)
    {
        if (max <= 0)
        {
            return 0m;
        }

        return Clamp((value / max) * 100m);
    }

    private static decimal Clamp(decimal value)
    {
        return Math.Max(0m, Math.Min(100m, value));
    }
}
