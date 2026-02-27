using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure.Providers;

public sealed class CricketDataOrgProvider : ICricketProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CricketDataOrgApiOptions _options;
    private readonly ILogger<CricketDataOrgProvider> _logger;

    public CricketDataOrgProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<CricketDataOrgApiOptions> options,
        ILogger<CricketDataOrgProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "CricketDataOrg";

    public async Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("CricketDataOrgApi key is not configured. Skipping provider.");
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient("CricketDataOrg");
            var endpoint = $"/v1/currentMatches?apikey={Uri.EscapeDataString(_options.ApiKey)}&offset=0";
            var payload = await client.GetFromJsonAsync<CricApiCurrentMatchesResponse>(endpoint, cancellationToken);

            if (payload?.Data is null || payload.Data.Count == 0)
            {
                return [];
            }

            var results = new List<ProviderUpcomingMatch>();
            foreach (var match in payload.Data)
            {
                var startTimeUtc = ParseStartTimeUtc(match.DateTimeGmt, match.Date);
                if (startTimeUtc is null || startTimeUtc < fromUtc || startTimeUtc > toUtc)
                {
                    continue;
                }

                var teamNames = (match.Teams ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                if (teamNames.Count < 2)
                {
                    continue;
                }

                var homeTeamName = teamNames[0];
                var awayTeamName = teamNames[1];

                var homeInfo = match.TeamInfo?.FirstOrDefault(x => string.Equals(x.Name, homeTeamName, StringComparison.OrdinalIgnoreCase));
                var awayInfo = match.TeamInfo?.FirstOrDefault(x => string.Equals(x.Name, awayTeamName, StringComparison.OrdinalIgnoreCase));

                ParseVenue(match.Venue, out var venueName, out var city);
                var country = InferCountry(match.Name, homeTeamName, awayTeamName, city);
                var (latitude, longitude) = BuildPseudoGeo(city, homeTeamName, awayTeamName);

                var format = ParseFormat(match.MatchType, match.Name);
                var status = ParseStatus(match.Status);

                results.Add(new ProviderUpcomingMatch(
                    ExternalId: $"cricapi-{match.Id}",
                    Format: format,
                    StartTimeUtc: startTimeUtc.Value,
                    Status: status,
                    Venue: new ProviderVenue(
                        ExternalId: $"cricapi-venue-{NormalizeId(venueName)}-{NormalizeId(city)}",
                        Name: venueName,
                        City: city,
                        Country: country,
                        Latitude: latitude,
                        Longitude: longitude),
                    HomeTeam: new ProviderTeam(
                        ExternalId: $"cricapi-team-{NormalizeId(homeTeamName)}",
                        Name: homeTeamName,
                        Country: country,
                        ShortName: NormalizeShortName(homeInfo?.ShortName, homeTeamName)),
                    AwayTeam: new ProviderTeam(
                        ExternalId: $"cricapi-team-{NormalizeId(awayTeamName)}",
                        Name: awayTeamName,
                        Country: country,
                        ShortName: NormalizeShortName(awayInfo?.ShortName, awayTeamName))));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CricketDataOrg provider failed. Returning no data to allow fallback providers.");
            return [];
        }
    }

    private static DateTimeOffset? ParseStartTimeUtc(string? dateTimeGmt, string? dateOnly)
    {
        if (!string.IsNullOrWhiteSpace(dateTimeGmt) && DateTimeOffset.TryParse(dateTimeGmt, out var parsedDateTime))
        {
            return parsedDateTime.ToUniversalTime();
        }

        if (!string.IsNullOrWhiteSpace(dateOnly) && DateTime.TryParse(dateOnly, out var parsedDate))
        {
            return new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, 12, 0, 0, TimeSpan.Zero);
        }

        return null;
    }

    private static void ParseVenue(string? venueRaw, out string venueName, out string city)
    {
        var venue = string.IsNullOrWhiteSpace(venueRaw) ? "Unknown Venue, Unknown City" : venueRaw.Trim();
        var parts = venue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            venueName = parts[0];
            city = parts[1];
            return;
        }

        venueName = parts.FirstOrDefault() ?? "Unknown Venue";
        city = "Unknown City";
    }

    private static string InferCountry(string? matchName, string homeTeam, string awayTeam, string city)
    {
        var text = $"{matchName} {homeTeam} {awayTeam} {city}".ToLowerInvariant();

        if (text.Contains("india")) return "India";
        if (text.Contains("england")) return "England";
        if (text.Contains("australia")) return "Australia";
        if (text.Contains("south africa")) return "South Africa";
        if (text.Contains("west indies")) return "West Indies";
        if (text.Contains("pakistan")) return "Pakistan";
        if (text.Contains("new zealand")) return "New Zealand";
        if (text.Contains("sri lanka")) return "Sri Lanka";
        if (text.Contains("bangladesh")) return "Bangladesh";

        return "Unknown";
    }

    private static (decimal Latitude, decimal Longitude) BuildPseudoGeo(string city, string homeTeam, string awayTeam)
    {
        var hash = Math.Abs(HashCode.Combine(city.ToLowerInvariant(), homeTeam.ToLowerInvariant(), awayTeam.ToLowerInvariant()));
        var latitude = (hash % 18000) / 100m - 90m;
        var longitude = ((hash / 18000) % 36000) / 100m - 180m;
        return (latitude, longitude);
    }

    private static MatchFormat ParseFormat(string? matchType, string? name)
    {
        var value = (matchType ?? name ?? string.Empty).ToLowerInvariant();
        if (value.Contains("test")) return MatchFormat.Test;
        if (value.Contains("t20")) return MatchFormat.T20;
        return MatchFormat.ODI;
    }

    private static MatchStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return MatchStatus.Scheduled;
        }

        var value = status.ToLowerInvariant();
        if (value.Contains("won") || value.Contains("draw") || value.Contains("completed"))
        {
            return MatchStatus.Completed;
        }

        if (value.Contains("live") || value.Contains("inning") || value.Contains("day"))
        {
            return MatchStatus.Live;
        }

        if (value.Contains("cancel") || value.Contains("abandon"))
        {
            return MatchStatus.Cancelled;
        }

        return MatchStatus.Scheduled;
    }

    private static string NormalizeShortName(string? provided, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return provided.Trim().ToUpperInvariant();
        }

        var letters = fallbackName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(3)
            .ToArray();

        if (letters.Length > 0)
        {
            return new string(letters).PadRight(3, 'X');
        }

        return "TBD";
    }

    private static string NormalizeId(string value)
    {
        var normalized = new string(value
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(normalized) ? "unknown" : normalized;
    }

    private sealed class CricApiCurrentMatchesResponse
    {
        [JsonPropertyName("data")]
        public List<CricApiMatch>? Data { get; init; }
    }

    private sealed class CricApiMatch
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("matchType")]
        public string? MatchType { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("venue")]
        public string? Venue { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("dateTimeGMT")]
        public string? DateTimeGmt { get; init; }

        [JsonPropertyName("teams")]
        public List<string>? Teams { get; init; }

        [JsonPropertyName("teamInfo")]
        public List<CricApiTeamInfo>? TeamInfo { get; init; }
    }

    private sealed class CricApiTeamInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("shortname")]
        public string? ShortName { get; init; }
    }
}
