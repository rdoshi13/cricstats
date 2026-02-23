namespace CricStats.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string SourceProvider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedAtUtc { get; set; }

    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    public ICollection<InningsScore> InningsScores { get; set; } = new List<InningsScore>();
}
