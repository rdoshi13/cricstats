using CricStats.Application.Models.Weather;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Services.Weather;

namespace CricStats.UnitTests;

public sealed class WeatherRiskCalculatorTests
{
    [Fact]
    public void Compute_WithLowSignalForecasts_ReturnsLowRisk()
    {
        var forecasts = new List<WeatherForecastPoint>
        {
            new("a", DateTimeOffset.UtcNow, 26m, 45m, 8m, 10m, 0.2m),
            new("b", DateTimeOffset.UtcNow.AddHours(1), 25m, 50m, 10m, 12m, 0.3m)
        };

        var result = WeatherRiskCalculator.Compute(forecasts, precipAmountMaxMm: 20m, windSpeedMaxKph: 60m);

        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.InRange(result.CompositeRiskScore, 0m, 33m);
    }

    [Fact]
    public void Compute_WithHighSignalForecasts_ReturnsHighRisk()
    {
        var forecasts = new List<WeatherForecastPoint>
        {
            new("a", DateTimeOffset.UtcNow, 24m, 95m, 55m, 95m, 16m),
            new("b", DateTimeOffset.UtcNow.AddHours(1), 23m, 90m, 52m, 90m, 18m)
        };

        var result = WeatherRiskCalculator.Compute(forecasts, precipAmountMaxMm: 20m, windSpeedMaxKph: 60m);

        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.InRange(result.CompositeRiskScore, 67m, 100m);
    }

    [Fact]
    public void Compute_WithEmptyForecasts_ReturnsZeroLow()
    {
        var result = WeatherRiskCalculator.Compute([], precipAmountMaxMm: 20m, windSpeedMaxKph: 60m);

        Assert.Equal(0m, result.CompositeRiskScore);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
    }
}
