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

        Assert.Equal("TestCricket", json.RootElement.GetProperty("providerUsed").GetString());
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
}
