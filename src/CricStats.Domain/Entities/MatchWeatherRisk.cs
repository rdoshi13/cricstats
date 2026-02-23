using CricStats.Domain.Enums;

namespace CricStats.Domain.Entities;

public sealed class MatchWeatherRisk
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public decimal CompositeRiskScore { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public DateTimeOffset ComputedAtUtc { get; set; }
}
