namespace CricStats.Contracts.Matches;

public sealed record UpcomingMatchesResponse(
    IReadOnlyList<UpcomingMatchItem> Matches,
    int TotalCount);
