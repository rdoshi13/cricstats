namespace CricStats.Application.Models.Weather;

public sealed record RefreshWeatherRiskResult(
    string? ProviderUsed,
    int MatchesProcessed,
    int RisksUpdated,
    DateTimeOffset RefreshedAtUtc);
