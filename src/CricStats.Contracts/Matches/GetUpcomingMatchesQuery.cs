namespace CricStats.Contracts.Matches;

public sealed class GetUpcomingMatchesQuery
{
    public string? Country { get; set; }
    public string? Format { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
