namespace CricStats.Contracts.Series;

public sealed record UpcomingSeriesItem(
    Guid SeriesId,
    string ExternalId,
    string Name,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc,
    string SourceProvider,
    IReadOnlyList<SeriesUpcomingMatchItem> Matches);
