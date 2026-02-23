using CricStats.Domain.Enums;

namespace CricStats.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; }
    public MatchFormat Format { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;
    public Guid AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;
    public MatchStatus Status { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedAtUtc { get; set; }

    public ICollection<InningsScore> InningsScores { get; set; } = new List<InningsScore>();
    public MatchWeatherRisk? MatchWeatherRisk { get; set; }
}
