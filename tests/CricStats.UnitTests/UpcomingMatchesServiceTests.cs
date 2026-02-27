using CricStats.Application.Interfaces.Providers;
using CricStats.Application.Interfaces;
using CricStats.Application.Models;
using CricStats.Application.Models.Providers;
using CricStats.Domain.Entities;
using CricStats.Domain.Enums;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Persistence;
using CricStats.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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

        Assert.Equal("FixtureCricketProvider", result.ProviderUsed);
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

    [Fact]
    public async Task GetUpcomingMatchesAsync_InDevelopment_ExcludesFixtureSourceProviderRows()
    {
        await using var dbContext = CreateDbContext();
        SeedSingleMatch(dbContext, "FixtureCricketProvider", "fixture-match");
        SeedSingleMatch(dbContext, "CricketDataOrg", "live-match");
        await dbContext.SaveChangesAsync();

        var upcomingService = new UpcomingMatchesService(
            dbContext,
            new NoOpSyncService(),
            NullLogger<UpcomingMatchesService>.Instance,
            new FixedHostEnvironment("Development"));

        var result = await upcomingService.GetUpcomingMatchesAsync(
            new UpcomingMatchesFilter(null, null, null, null));

        Assert.Single(result.Matches);
        Assert.Equal("live-match", result.Matches[0].VenueName);
    }

    [Fact]
    public async Task SyncUpcomingMatchesAsync_InDevelopment_SkipsFixtureProvider()
    {
        await using var dbContext = CreateDbContext();

        var providers = new ICricketProvider[]
        {
            new FixtureCricketProvider()
        };

        var options = Options.Create(new CricketProvidersOptions
        {
            Priority = ["FixtureCricketProvider"],
            SyncWindowDays = 14
        });

        var syncService = new UpcomingMatchesSyncService(
            dbContext,
            providers,
            options,
            NullLogger<UpcomingMatchesSyncService>.Instance,
            new FixedHostEnvironment("Development"));

        var result = await syncService.SyncUpcomingMatchesAsync();

        Assert.Null(result.ProviderUsed);
        Assert.Equal(0, result.MatchesInserted);
        Assert.Equal(0, await dbContext.Matches.CountAsync());
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
            new FixtureCricketProvider()
        };

        var options = Options.Create(new CricketProvidersOptions
        {
            Priority = ["FixtureCricketProvider"],
            SyncWindowDays = 14
        });

        return new UpcomingMatchesSyncService(
            dbContext,
            providers,
            options,
            NullLogger<UpcomingMatchesSyncService>.Instance);
    }

    private static void SeedSingleMatch(
        CricStatsDbContext dbContext,
        string sourceProvider,
        string venueName)
    {
        var teamA = new Team
        {
            Id = Guid.NewGuid(),
            Name = $"{venueName}-Team-A",
            Country = "India",
            ShortName = "A",
            SourceProvider = sourceProvider,
            ExternalId = $"{venueName}-team-a",
            LastSyncedAtUtc = DateTimeOffset.UtcNow
        };

        var teamB = new Team
        {
            Id = Guid.NewGuid(),
            Name = $"{venueName}-Team-B",
            Country = "India",
            ShortName = "B",
            SourceProvider = sourceProvider,
            ExternalId = $"{venueName}-team-b",
            LastSyncedAtUtc = DateTimeOffset.UtcNow
        };

        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = venueName,
            City = "Mumbai",
            Country = "India",
            Latitude = 19m,
            Longitude = 72m,
            SourceProvider = sourceProvider,
            ExternalId = $"{venueName}-venue",
            LastSyncedAtUtc = DateTimeOffset.UtcNow
        };

        var match = new Match
        {
            Id = Guid.NewGuid(),
            Format = MatchFormat.T20,
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(1),
            VenueId = venue.Id,
            HomeTeamId = teamA.Id,
            AwayTeamId = teamB.Id,
            Status = MatchStatus.Scheduled,
            SourceProvider = sourceProvider,
            ExternalId = $"{venueName}-match",
            LastSyncedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Teams.AddRange(teamA, teamB);
        dbContext.Venues.Add(venue);
        dbContext.Matches.Add(match);
    }

    private sealed class NoOpSyncService : IUpcomingMatchesSyncService
    {
        public Task<UpcomingMatchesSyncResult> SyncUpcomingMatchesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new UpcomingMatchesSyncResult(
                ProviderUsed: null,
                ProvidersTried: [],
                MatchesInserted: 0,
                MatchesUpdated: 0,
                TeamsUpserted: 0,
                VenuesUpserted: 0,
                SyncedAtUtc: DateTimeOffset.UtcNow));
        }
    }

    private sealed class FixedHostEnvironment : IHostEnvironment
    {
        public FixedHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = nameof(CricStats);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    // Deterministic fixtures used by unit tests only.
    private sealed class FixtureCricketProvider : ICricketProvider
    {
        public string Name => "FixtureCricketProvider";

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
                    ExternalId: "fixture-match-ind-aus-t20",
                    Format: MatchFormat.T20,
                    StartTimeUtc: baseDay.AddDays(1).AddHours(14),
                    Status: MatchStatus.Scheduled,
                    Venue: new ProviderVenue("fixture-venue-wankhede", "Wankhede Stadium", "Mumbai", "India", 18.9389m, 72.8258m),
                    HomeTeam: new ProviderTeam("fixture-team-india", "India", "India", "IND"),
                    AwayTeam: new ProviderTeam("fixture-team-australia", "Australia", "Australia", "AUS")),
                new(
                    ExternalId: "fixture-match-wi-eng-odi",
                    Format: MatchFormat.ODI,
                    StartTimeUtc: baseDay.AddDays(3).AddHours(9).AddMinutes(30),
                    Status: MatchStatus.Scheduled,
                    Venue: new ProviderVenue("fixture-venue-kensington", "Kensington Oval", "Bridgetown", "West Indies", 13.1045m, -59.6133m),
                    HomeTeam: new ProviderTeam("fixture-team-west-indies", "West Indies", "West Indies", "WI"),
                    AwayTeam: new ProviderTeam("fixture-team-england", "England", "England", "ENG"))
            };

            var filtered = matches
                .Where(x => x.StartTimeUtc >= fromUtc && x.StartTimeUtc <= toUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>(filtered);
        }
    }
}
