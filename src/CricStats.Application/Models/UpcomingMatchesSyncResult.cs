namespace CricStats.Application.Models;

public sealed record UpcomingMatchesSyncResult(
    string? ProviderUsed,
    IReadOnlyList<string> ProvidersTried,
    int MatchesInserted,
    int MatchesUpdated,
    int TeamsUpserted,
    int VenuesUpserted,
    DateTimeOffset SyncedAtUtc);
