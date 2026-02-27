using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CricStats.IntegrationTests;

public sealed class MatchesUpcomingEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MatchesUpcomingEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUpcomingMatches_ReturnsOkAndExpectedSchema()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/matches/upcoming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.True(json.RootElement.TryGetProperty("matches", out var matches));
        Assert.True(json.RootElement.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(JsonValueKind.Array, matches.ValueKind);
        Assert.True(totalCount.GetInt32() >= 1);

        var firstMatch = matches[0];
        Assert.True(firstMatch.TryGetProperty("matchId", out _));
        Assert.True(firstMatch.TryGetProperty("weatherRisk", out _));
    }

    [Fact]
    public async Task GetUpcomingMatches_WithFormatFilter_ReturnsOnlyRequestedFormat()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/matches/upcoming?format=T20");

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        var matches = json.RootElement.GetProperty("matches");

        Assert.Equal(1, matches.GetArrayLength());
        Assert.Equal("T20", matches[0].GetProperty("format").GetString());
    }

    [Fact]
    public async Task GetUpcomingMatches_WithInvalidFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/matches/upcoming?format=INVALID");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SyncUpcomingMatchesEndpoint_ReturnsProviderDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/admin/sync/upcoming", content: null);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.Equal("FixtureCricketProvider", json.RootElement.GetProperty("providerUsed").GetString());
        Assert.True(json.RootElement.GetProperty("matchesInserted").GetInt32() >= 0);
        Assert.True(json.RootElement.GetProperty("providersTried").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task RefreshWeatherRiskEndpoint_ReturnsSummary()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/admin/weather/refresh", content: null);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.Equal("OpenMeteoStub", json.RootElement.GetProperty("providerUsed").GetString());
        Assert.True(json.RootElement.GetProperty("matchesProcessed").GetInt32() >= 1);
        Assert.True(json.RootElement.GetProperty("risksUpdated").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetMatchWeatherRisk_ReturnsRiskAndBreakdown()
    {
        var client = _factory.CreateClient();

        var upcomingResponse = await client.GetAsync("/api/v1/matches/upcoming");
        upcomingResponse.EnsureSuccessStatusCode();

        await using var stream = await upcomingResponse.Content.ReadAsStreamAsync();
        using var upcomingJson = await JsonDocument.ParseAsync(stream);
        var matchId = upcomingJson.RootElement
            .GetProperty("matches")[0]
            .GetProperty("matchId")
            .GetGuid();

        var weatherResponse = await client.GetAsync($"/api/v1/matches/{matchId}/weather-risk");

        weatherResponse.EnsureSuccessStatusCode();

        await using var weatherStream = await weatherResponse.Content.ReadAsStreamAsync();
        using var weatherJson = await JsonDocument.ParseAsync(weatherStream);

        Assert.Equal(matchId, weatherJson.RootElement.GetProperty("matchId").GetGuid());
        Assert.True(weatherJson.RootElement.GetProperty("compositeRiskScore").GetDecimal() >= 0m);
        Assert.True(weatherJson.RootElement.GetProperty("breakdown").TryGetProperty("averagePrecipProbability", out _));
    }

    [Fact]
    public async Task SyncUpcomingSeriesEndpoint_ReturnsProviderDetails()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/admin/sync/series", content: null);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.Equal("FixtureCricketProvider", json.RootElement.GetProperty("providerUsed").GetString());
        Assert.True(json.RootElement.GetProperty("seriesUpserted").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetUpcomingSeries_ReturnsSeriesWithMatches()
    {
        var client = _factory.CreateClient();
        var syncResponse = await client.PostAsync("/api/v1/admin/sync/series", content: null);
        syncResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/series/upcoming");

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.True(json.RootElement.TryGetProperty("series", out var series));
        Assert.True(json.RootElement.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 1);

        var firstSeries = series[0];
        Assert.True(firstSeries.TryGetProperty("name", out _));
        Assert.True(firstSeries.TryGetProperty("matches", out var matches));
        Assert.Equal(JsonValueKind.Array, matches.ValueKind);
        Assert.True(matches[0].TryGetProperty("venueName", out _));
        Assert.True(matches[0].TryGetProperty("venueCountry", out _));
    }

    [Fact]
    public async Task GetSeriesById_ReturnsSeriesDetailsWithPagination()
    {
        var client = _factory.CreateClient();
        var syncResponse = await client.PostAsync("/api/v1/admin/sync/series", content: null);
        syncResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync("/api/v1/series/upcoming");
        listResponse.EnsureSuccessStatusCode();

        await using var listStream = await listResponse.Content.ReadAsStreamAsync();
        using var listJson = await JsonDocument.ParseAsync(listStream);
        var seriesId = listJson.RootElement.GetProperty("series")[0].GetProperty("seriesId").GetGuid();

        var response = await client.GetAsync($"/api/v1/series/{seriesId}?page=1&pageSize=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        Assert.Equal(seriesId, json.RootElement.GetProperty("seriesId").GetGuid());
        Assert.Equal(1, json.RootElement.GetProperty("matchPage").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("matchPageSize").GetInt32());
        Assert.True(json.RootElement.GetProperty("totalMatchCount").GetInt32() >= 1);
        Assert.Equal(1, json.RootElement.GetProperty("matches").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("matches")[0].TryGetProperty("venueName", out _));
        Assert.True(json.RootElement.GetProperty("matches")[0].TryGetProperty("venueCountry", out _));
    }

    [Fact]
    public async Task GetSeriesById_UnknownSeries_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var unknownSeriesId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/v1/series/{unknownSeriesId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
