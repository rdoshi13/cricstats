namespace CricStats.Application.Models;

public sealed record SeriesSyncResult(
    string? ProviderUsed,
    IReadOnlyList<string> ProvidersTried,
    int SeriesUpserted,
    int SeriesMatchesUpserted,
    DateTimeOffset SyncedAtUtc);
