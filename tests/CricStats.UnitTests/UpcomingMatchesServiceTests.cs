using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Models;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
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

        Assert.Equal("TestCricket", result.ProviderUsed);
        Assert.Equal(2, result.MatchesInserted);
        Assert.Equal(0, result.MatchesUpdated);
        Assert.Equal(4, result.TeamsUpserted);
        Assert.Equal(2, result.VenuesUpserted);

        var persistedMatches = await dbContext.Matches.CountAsync();
        Assert.Equal(2, persistedMatches);
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
        var from = baseDay.AddDays(2);
        var to = baseDay.AddDays(4).AddHours(23).AddMinutes(59);

        var result = await upcomingService.GetUpcomingMatchesAsync(
            new UpcomingMatchesFilter("West Indies", MatchFormat.ODI, from, to));

        Assert.Single(result.Matches);
        Assert.Equal("ODI", result.Matches[0].Format);
        Assert.Equal("West Indies", result.Matches[0].VenueCountry);
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
            new TestCricketProvider()
        };

        var options = Options.Create(new CricketProvidersOptions
        {
            Priority = ["TestCricket"],
            SyncWindowDays = 14
        });

        return new UpcomingMatchesSyncService(
            dbContext,
            providers,
            options,
            NullLogger<UpcomingMatchesSyncService>.Instance);
    }

    private sealed class TestCricketProvider : ICricketProvider
    {
        public string Name => "TestCricket";

        public Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var baseDay = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);

            var matches = new List<ProviderUpcomingMatch>
            {
                new(
                    ExternalId: "test-match-001",
                    Format: MatchFormat.T20,
                    StartTimeUtc: baseDay.AddDays(1).AddHours(14),
                    Status: MatchStatus.Scheduled,
                    Venue: new ProviderVenue("test-venue-001", "Wankhede Stadium", "Mumbai", "India", 18.9389m, 72.8258m),
                    HomeTeam: new ProviderTeam("test-team-001", "India", "India", "IND"),
                    AwayTeam: new ProviderTeam("test-team-002", "Australia", "Australia", "AUS")),
                new(
                    ExternalId: "test-match-002",
                    Format: MatchFormat.ODI,
                    StartTimeUtc: baseDay.AddDays(3).AddHours(9).AddMinutes(30),
                    Status: MatchStatus.Scheduled,
                    Venue: new ProviderVenue("test-venue-002", "Kensington Oval", "Bridgetown", "West Indies", 13.1045m, -59.6133m),
                    HomeTeam: new ProviderTeam("test-team-003", "West Indies", "West Indies", "WI"),
                    AwayTeam: new ProviderTeam("test-team-004", "England", "England", "ENG"))
            };

            var filtered = matches
                .Where(x => x.StartTimeUtc >= fromUtc && x.StartTimeUtc <= toUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
        }
    }
}
