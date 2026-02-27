namespace CricStats.Infrastructure.Options;

public sealed class CricketDataOrgApiOptions
{
    public const string SectionName = "CricketDataOrgApi";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://api.cricapi.com";
    public string ApiKey { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 8;
}
