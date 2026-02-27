namespace CricStats.Contracts.Admin;

public sealed record RefreshWeatherRiskResponse(
    string? ProviderUsed,
    int MatchesProcessed,
    int RisksUpdated,
    DateTimeOffset RefreshedAtUtc);
