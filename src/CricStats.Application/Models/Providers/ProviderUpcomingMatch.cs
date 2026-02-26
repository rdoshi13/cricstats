using CricStats.Domain.Enums;

namespace CricStats.Application.Models.Providers;

public sealed record ProviderUpcomingMatch(
    string ExternalId,
    MatchFormat Format,
    DateTimeOffset StartTimeUtc,
    MatchStatus Status,
    ProviderVenue Venue,
    ProviderTeam HomeTeam,
    ProviderTeam AwayTeam);
