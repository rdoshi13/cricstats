namespace CricStats.Contracts.Matches;

public sealed record UpcomingMatchItem(
    Guid MatchId,
    string Format,
    DateTimeOffset StartTimeUtc,
    Guid VenueId,
    string VenueName,
    string VenueCountry,
    Guid HomeTeamId,
    string HomeTeamName,
    string HomeTeamCountry,
    Guid AwayTeamId,
    string AwayTeamName,
    string AwayTeamCountry,
    string Status,
    WeatherRiskSummaryDto WeatherRisk);
