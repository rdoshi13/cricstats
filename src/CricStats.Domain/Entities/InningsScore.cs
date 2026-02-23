namespace CricStats.Domain.Entities;

public sealed class InningsScore
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int InningsNo { get; set; }
    public int Runs { get; set; }
    public int Wickets { get; set; }
    public decimal Overs { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedAtUtc { get; set; }
}
