using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
using CricStats.Infrastructure.Providers;
using CricStats.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CricStats.UnitTests;

public sealed class UpcomingMatchesServiceTests
{
    [Fact]
    public async Task SyncUpcomingMatchesAsync_UsesPriorityProviderAndPersistsFixtures()
    {
        await using var dbContext = CreateDbContext();
        var syncService = CreateSyncService(dbContext);

        var result = await syncService.SyncUpcomingMatchesAsync();

        Assert.Equal("CricketDataOrg", result.ProviderUsed);
        Assert.Equal(3, result.MatchesInserted);
        Assert.Equal(0, result.MatchesUpdated);
        Assert.Equal(5, result.TeamsUpserted);
        Assert.Equal(3, result.VenuesUpserted);

        var persistedMatches = await dbContext.Matches.CountAsync();
        Assert.Equal(3, persistedMatches);
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithCountryFilter_ReturnsMatchesForVenueOrTeams()
    {
        await using var dbContext = CreateDbContext();
        var syncService = CreateSyncService(dbContext);
        await syncService.SyncUpcomingMatchesAsync();

        var upcomingService = new UpcomingMatchesService(
            dbContext,
            syncService,
            NullLogger<UpcomingMatchesService>.Instance);

        var result = await upcomingService.GetUpcomingMatchesAsync(
            new UpcomingMatchesFilter("India", null, null, null));

        Assert.Single(result.Matches);
        Assert.Equal("India", result.Matches[0].VenueCountry);
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithFormatFilter_ReturnsOnlyMatchingFormat()
    {
        await using var dbContext = CreateDbContext();
        var syncService = CreateSyncService(dbContext);
        await syncService.SyncUpcomingMatchesAsync();

        var upcomingService = new UpcomingMatchesService(
            dbContext,
            syncService,
            NullLogger<UpcomingMatchesService>.Instance);

        var result = await upcomingService.GetUpcomingMatchesAsync(
            new UpcomingMatchesFilter(null, MatchFormat.ODI, null, null));

        Assert.Single(result.Matches);
        Assert.All(result.Matches, match => Assert.Equal("ODI", match.Format));
    }

    [Fact]
    public async Task GetUpcomingMatchesAsync_WithCombinedFilters_ReturnsIntersection()
    {
        await using var dbContext = CreateDbContext();
        var syncService = CreateSyncService(dbContext);
        await syncService.SyncUpcomingMatchesAsync();

        var upcomingService = new UpcomingMatchesService(
            dbContext,
            syncService,
            NullLogger<UpcomingMatchesService>.Instance);

        var baseDay = DateTimeOffset.UtcNow.Date;
        var from = baseDay.AddDays(6);
        var to = baseDay.AddDays(8).AddHours(23).AddMinutes(59);

        var result = await upcomingService.GetUpcomingMatchesAsync(
            new UpcomingMatchesFilter("England", MatchFormat.Test, from, to));

        Assert.Single(result.Matches);
        Assert.Equal("Test", result.Matches[0].Format);
        Assert.Equal("England", result.Matches[0].VenueCountry);
    }

    private static CricStatsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CricStatsDbContext>()
            .UseInMemoryDatabase($"CricStatsUnitTests-{Guid.NewGuid()}")
            .Options;

        return new CricStatsDbContext(options);
    }

    private static UpcomingMatchesSyncService CreateSyncService(CricStatsDbContext dbContext)
    {
        var providers = new ICricketProvider[]
        {
            new CricketDataOrgProvider(),
            new ApiSportsProvider()
        };

        var options = Options.Create(new CricketProvidersOptions
        {
            Priority = ["CricketDataOrg", "ApiSports"],
            SyncWindowDays = 14
        });

        return new UpcomingMatchesSyncService(
            dbContext,
            providers,
            options,
            NullLogger<UpcomingMatchesSyncService>.Instance);
    }
}
