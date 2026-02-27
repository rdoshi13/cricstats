namespace CricStats.Contracts.Series;

public sealed record GetUpcomingSeriesQuery(
    DateTimeOffset? From,
    DateTimeOffset? To);
