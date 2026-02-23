namespace CricStats.Contracts.Matches;

public sealed record WeatherRiskSummaryDto(
    decimal CompositeRiskScore,
    string RiskLevel);
