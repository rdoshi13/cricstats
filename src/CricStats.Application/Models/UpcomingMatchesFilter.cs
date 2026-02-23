using CricStats.Domain.Enums;

namespace CricStats.Application.Models;

public sealed record UpcomingMatchesFilter(
    string? Country,
    MatchFormat? Format,
    DateTimeOffset? From,
    DateTimeOffset? To);
