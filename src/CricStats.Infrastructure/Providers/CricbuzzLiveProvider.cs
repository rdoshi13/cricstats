using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure.Providers;

public sealed class CricbuzzLiveProvider : ICricketProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LiveCricketOptions _options;
    private readonly ILogger<CricbuzzLiveProvider> _logger;
    private readonly Dictionary<string, GeoResult> _geoCache = new(StringComparer.OrdinalIgnoreCase);

    public CricbuzzLiveProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<LiveCricketOptions> options,
        ILogger<CricbuzzLiveProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "CricbuzzLive";

    public async Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient("CricbuzzLive");
            var endpoint = $"/v1/matches/upcoming?type={Uri.EscapeDataString(_options.MatchType)}";

            var payload = await client.GetFromJsonAsync<CricbuzzUpcomingResponse>(endpoint, cancellationToken);
            if (payload?.Data?.Matches is null || payload.Data.Matches.Count == 0)
            {
                return [];
            }

            var results = new List<ProviderUpcomingMatch>();
            for (var index = 0; index < payload.Data.Matches.Count; index++)
            {
                var match = payload.Data.Matches[index];

                var matchTitle = string.IsNullOrWhiteSpace(match.Title)
                    ? $"Match {match.Id ?? index.ToString()}"
                    : match.Title.Trim();

                ParseTeams(matchTitle, out var homeTeamName, out var awayTeamName);
                ParsePlace(match.TimeAndPlace?.Place, out var city, out var venueName);
                var startTimeUtc = EstimateStartTimeUtc(match.TimeAndPlace?.Date, match.TimeAndPlace?.Time, index);

                if (startTimeUtc < fromUtc || startTimeUtc > toUtc)
                {
                    continue;
                }

                var format = ParseFormat(matchTitle);
                var geo = await ResolveGeoAsync(city, cancellationToken);

                var homeShort = BuildShortName(homeTeamName);
                var awayShort = BuildShortName(awayTeamName);

                var matchIdToken = string.IsNullOrWhiteSpace(match.Id) ? $"idx-{index}" : match.Id;
                results.Add(new ProviderUpcomingMatch(
                    ExternalId: $"cricbuzz-{matchIdToken}",
                    Format: format,
                    StartTimeUtc: startTimeUtc,
                    Status: MatchStatus.Scheduled,
                    Venue: new ProviderVenue(
                        ExternalId: $"cricbuzz-venue-{matchIdToken}",
                        Name: venueName,
                        City: city,
                        Country: geo.Country,
                        Latitude: geo.Latitude,
                        Longitude: geo.Longitude),
                    HomeTeam: new ProviderTeam(
                        ExternalId: $"cricbuzz-team-{NormalizeId(homeTeamName)}",
                        Name: homeTeamName,
                        Country: geo.Country,
                        ShortName: homeShort),
                    AwayTeam: new ProviderTeam(
                        ExternalId: $"cricbuzz-team-{NormalizeId(awayTeamName)}",
                        Name: awayTeamName,
                        Country: geo.Country,
                        ShortName: awayShort)));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cricbuzz live provider failed. Returning no fixtures to allow fallback providers.");
            return [];
        }
    }

    private async Task<GeoResult> ResolveGeoAsync(string city, CancellationToken cancellationToken)
    {
        if (_geoCache.TryGetValue(city, out var cached))
        {
            return cached;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("OpenMeteoGeocoding");
            var endpoint = $"/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";
            var response = await client.GetFromJsonAsync<OpenMeteoGeoResponse>(endpoint, cancellationToken);
            var result = response?.Results?.FirstOrDefault();

            if (result is not null)
            {
                var geo = new GeoResult(
                    Latitude: Convert.ToDecimal(result.Latitude),
                    Longitude: Convert.ToDecimal(result.Longitude),
                    Country: string.IsNullOrWhiteSpace(result.Country) ? "Unknown" : result.Country);
                _geoCache[city] = geo;
                return geo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Geocoding failed for city {City}. Falling back to deterministic pseudo coordinates.", city);
        }

        var pseudo = BuildPseudoGeo(city);
        _geoCache[city] = pseudo;
        return pseudo;
    }

    private static GeoResult BuildPseudoGeo(string city)
    {
        var hash = Math.Abs(city.GetHashCode(StringComparison.OrdinalIgnoreCase));
        var latitude = (hash % 18000) / 100m - 90m;
        var longitude = ((hash / 18000) % 36000) / 100m - 180m;
        return new GeoResult(latitude, longitude, "Unknown");
    }

    private static string BuildShortName(string teamName)
    {
        var letters = teamName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(3)
            .ToArray();

        if (letters.Length >= 2)
        {
            return new string(letters);
        }

        var compact = new string(teamName.Where(char.IsLetter).ToArray()).ToUpperInvariant();
        return compact.Length >= 3 ? compact[..3] : compact.PadRight(3, 'X');
    }

    private static string NormalizeId(string value)
    {
        return new string(value
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-')
            .ToArray())
            .Replace(' ', '-');
    }

    private static MatchFormat ParseFormat(string title)
    {
        var lower = title.ToLowerInvariant();
        if (lower.Contains("test"))
        {
            return MatchFormat.Test;
        }

        if (lower.Contains("t20"))
        {
            return MatchFormat.T20;
        }

        return MatchFormat.ODI;
    }

    private static void ParseTeams(string title, out string homeTeam, out string awayTeam)
    {
        var head = title.Split(',')[0].Trim();
        var tokens = head.Split(" vs ", StringSplitOptions.TrimEntries);

        if (tokens.Length == 2)
        {
            homeTeam = tokens[0];
            awayTeam = tokens[1];
            return;
        }

        homeTeam = "Team A";
        awayTeam = "Team B";
    }

    private static void ParsePlace(string? placeRaw, out string city, out string venueName)
    {
        var place = string.IsNullOrWhiteSpace(placeRaw)
            ? "Unknown City, Unknown Venue"
            : placeRaw.Replace("at ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        var parts = place.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            city = parts[0];
            venueName = string.Join(", ", parts.Skip(1));
            return;
        }

        city = parts.FirstOrDefault() ?? "Unknown City";
        venueName = parts.FirstOrDefault() ?? "Unknown Venue";
    }

    private static DateTimeOffset EstimateStartTimeUtc(string? dateRaw, string? timeRaw, int sequenceIndex)
    {
        if (!string.IsNullOrWhiteSpace(dateRaw) && DateTime.TryParse(dateRaw, out var parsedDate))
        {
            var hour = 10;
            var minute = 0;

            if (!string.IsNullOrWhiteSpace(timeRaw) && TimeOnly.TryParse(timeRaw, out var parsedTime))
            {
                hour = parsedTime.Hour;
                minute = parsedTime.Minute;
            }

            return new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, hour, minute, 0, TimeSpan.Zero);
        }

        var baseDate = DateTimeOffset.UtcNow.Date;
        return new DateTimeOffset(baseDate.Year, baseDate.Month, baseDate.Day, 12, 0, 0, TimeSpan.Zero)
            .AddDays(sequenceIndex + 1);
    }

    private sealed record GeoResult(decimal Latitude, decimal Longitude, string Country);

    private sealed class CricbuzzUpcomingResponse
    {
        [JsonPropertyName("data")]
        public CricbuzzData? Data { get; init; }
    }

    private sealed class CricbuzzData
    {
        [JsonPropertyName("matches")]
        public List<CricbuzzMatch>? Matches { get; init; }
    }

    private sealed class CricbuzzMatch
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("timeAndPlace")]
        public CricbuzzTimeAndPlace? TimeAndPlace { get; init; }
    }

    private sealed class CricbuzzTimeAndPlace
    {
        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("time")]
        public string? Time { get; init; }

        [JsonPropertyName("place")]
        public string? Place { get; init; }
    }

    private sealed class OpenMeteoGeoResponse
    {
        [JsonPropertyName("results")]
        public List<OpenMeteoGeoResult>? Results { get; init; }
    }

    private sealed class OpenMeteoGeoResult
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }
    }
}
