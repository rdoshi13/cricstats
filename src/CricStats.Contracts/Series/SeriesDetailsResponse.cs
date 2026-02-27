namespace CricStats.Contracts.Series;

public sealed record SeriesDetailsResponse(
    Guid SeriesId,
    string ExternalId,
    string Name,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc,
    string SourceProvider,
    int MatchPage,
    int MatchPageSize,
    int TotalMatchCount,
    IReadOnlyList<SeriesUpcomingMatchItem> Matches);
