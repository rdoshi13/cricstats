namespace CricStats.Contracts.Series;

public sealed record UpcomingSeriesResponse(
    IReadOnlyList<UpcomingSeriesItem> Series,
    int TotalCount);
