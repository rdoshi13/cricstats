using CricStats.Domain.Enums;

namespace CricStats.Application.Models.Providers;

public sealed record ProviderSeriesMatch(
    string ExternalId,
    string Name,
    string? VenueName,
    string? VenueCountry,
    MatchFormat? Format,
    DateTimeOffset? StartTimeUtc,
    MatchStatus? Status,
    string StatusText);
