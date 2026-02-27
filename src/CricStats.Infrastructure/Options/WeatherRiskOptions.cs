namespace CricStats.Infrastructure.Options;

public sealed class WeatherRiskOptions
{
    public const string SectionName = "WeatherRisk";

    public string ProviderName { get; init; } = "OpenMeteoStub";
    public int RefreshWindowDays { get; init; } = 14;
    public decimal PrecipAmountMaxMm { get; init; } = 20m;
    public decimal WindSpeedMaxKph { get; init; } = 60m;
}
