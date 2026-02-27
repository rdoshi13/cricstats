namespace CricStats.Infrastructure.Options;

public sealed class CricketProvidersOptions
{
    public const string SectionName = "CricketProviders";

    public List<string> Priority { get; init; } = [];
    public int SyncWindowDays { get; init; } = 14;
    public int SeriesSyncWindowDays { get; init; } = 120;
    public int SeriesInfoMaxConcurrency { get; init; } = 3;
    public int SeriesInfoMaxRetries { get; init; } = 2;
    public int SeriesInfoRetryDelayMs { get; init; } = 300;
}
