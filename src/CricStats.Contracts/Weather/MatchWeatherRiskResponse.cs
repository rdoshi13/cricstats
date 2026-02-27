namespace CricStats.Contracts.Weather;

public sealed record MatchWeatherRiskResponse(
    Guid MatchId,
    decimal CompositeRiskScore,
    string RiskLevel,
    DateTimeOffset ComputedAtUtc,
    WeatherRiskBreakdownDto Breakdown);
