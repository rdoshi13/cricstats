namespace CricStats.Infrastructure.Options;

public sealed class LiveCricketOptions
{
    public const string SectionName = "LiveCricket";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://cricbuzz-live.vercel.app";
    public string MatchType { get; init; } = "international";
    public int TimeoutSeconds { get; init; } = 8;
}
