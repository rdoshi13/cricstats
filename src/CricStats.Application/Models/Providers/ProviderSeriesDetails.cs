namespace CricStats.Application.Models.Providers;

public sealed record ProviderSeriesDetails(
    string ExternalId,
    string Name,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc,
    IReadOnlyList<ProviderSeriesMatch> Matches);
