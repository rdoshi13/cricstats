using CricStats.Application.Models;
using CricStats.Application.Services;
using CricStats.Domain.Enums;

namespace CricStats.UnitTests;

public sealed class UpcomingMatchesServiceTests
{
    private readonly UpcomingMatchesService _sut = new();

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithCountryFilter_ReturnsMatchesForCountryAcrossVenueAndTeams()
    {
        var filter = new UpcomingMatchesFilter("India", null, null, null);

        var result = await _sut.GetUpcomingMatchesAsync(filter);

        Assert.Single(result.Matches);
        Assert.Equal("India", result.Matches[0].VenueCountry);
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithFormatFilter_ReturnsOnlyMatchingFormat()
    {
        var filter = new UpcomingMatchesFilter(null, MatchFormat.ODI, null, null);

        var result = await _sut.GetUpcomingMatchesAsync(filter);

        Assert.Single(result.Matches);
        Assert.All(result.Matches, match => Assert.Equal("ODI", match.Format));
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithDateWindow_AppliesInclusiveBoundaries()
    {
        var filter = new UpcomingMatchesFilter(
            null,
            null,
            new DateTimeOffset(2026, 3, 5, 9, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 8, 4, 0, 0, TimeSpan.Zero));

        var result = await _sut.GetUpcomingMatchesAsync(filter);

        Assert.Equal(2, result.Matches.Count);
        Assert.Equal(new DateTimeOffset(2026, 3, 5, 9, 30, 0, TimeSpan.Zero), result.Matches[0].StartTimeUtc);
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 4, 0, 0, TimeSpan.Zero), result.Matches[1].StartTimeUtc);
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithCombinedFilters_ReturnsIntersection()
    {
        var filter = new UpcomingMatchesFilter(
            "England",
            MatchFormat.Test,
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero));

        var result = await _sut.GetUpcomingMatchesAsync(filter);

        Assert.Single(result.Matches);
        Assert.Equal("Test", result.Matches[0].Format);
        Assert.Equal("England", result.Matches[0].VenueCountry);
    }
}
