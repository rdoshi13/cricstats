namespace CricStats.Application.Models.Providers;

public sealed record ProviderTeam(
    string ExternalId,
    string Name,
    string Country,
    string ShortName);
