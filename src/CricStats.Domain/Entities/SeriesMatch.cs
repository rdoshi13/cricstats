using CricStats.Domain.Enums;

namespace CricStats.Domain.Entities;

public sealed class SeriesMatch
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? VenueName { get; set; }
    public string? VenueCountry { get; set; }
    public MatchFormat? Format { get; set; }
    public DateTimeOffset? StartTimeUtc { get; set; }
    public MatchStatus? Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string SourceProvider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedAtUtc { get; set; }
}
