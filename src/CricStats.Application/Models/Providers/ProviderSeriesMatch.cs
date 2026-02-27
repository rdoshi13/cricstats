using CricStats.Domain.Enums;

namespace CricStats.Application.Models.Providers;

public sealed record ProviderSeriesMatch(
    string ExternalId,
    string Name,
    MatchFormat? Format,
    DateTimeOffset? StartTimeUtc,
    MatchStatus? Status,
    string StatusText);
