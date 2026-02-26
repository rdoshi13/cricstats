namespace CricStats.Contracts.Admin;

public sealed record SyncUpcomingMatchesResponse(
    string? ProviderUsed,
    IReadOnlyList<string> ProvidersTried,
    int MatchesInserted,
    int MatchesUpdated,
    int TeamsUpserted,
    int VenuesUpserted,
    DateTimeOffset SyncedAtUtc);
