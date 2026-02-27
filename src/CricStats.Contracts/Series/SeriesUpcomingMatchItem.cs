namespace CricStats.Contracts.Series;

public sealed record SeriesUpcomingMatchItem(
    Guid SeriesMatchId,
    string ExternalId,
    string Name,
    string? Format,
    DateTimeOffset? StartTimeUtc,
    string? Status,
    string StatusText);
