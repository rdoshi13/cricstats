namespace CricStats.Contracts.Admin;

public sealed record SyncUpcomingSeriesResponse(
    string? ProviderUsed,
    IReadOnlyList<string> ProvidersTried,
    int SeriesUpserted,
    int SeriesMatchesUpserted,
    DateTimeOffset SyncedAtUtc);
