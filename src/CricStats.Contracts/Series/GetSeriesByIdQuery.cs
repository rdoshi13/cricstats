namespace CricStats.Contracts.Series;

public sealed record GetSeriesByIdQuery(
    int Page = 1,
    int PageSize = 20);
