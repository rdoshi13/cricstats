namespace CricStats.Application.Models.Providers;

public sealed record ProviderSeries(
    string ExternalId,
    string Name,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc);
