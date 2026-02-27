using CricStats.Application.Interfaces.Providers;
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

public sealed class SeriesSyncServiceTests
{
    [Fact]
    public async Task SyncUpcomingSeriesAsync_RemovesStaleUpcomingSeriesAndMatches()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        var staleSeries = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Stale Series",
            StartDateUtc = now.AddDays(2),
            EndDateUtc = now.AddDays(12),
            SourceProvider = "CricketDataOrg",
            ExternalId = "stale-series",
            LastSyncedAtUtc = now.AddDays(-2)
        };

        var oldSeries = new Series
        {
            Id = Guid.NewGuid(),
            Name = "Older Series",
            StartDateUtc = now.AddDays(-20),
            EndDateUtc = now.AddDays(-10),
            SourceProvider = "CricketDataOrg",
            ExternalId = "old-series",
            LastSyncedAtUtc = now.AddDays(-20)
        };

        dbContext.Series.AddRange(staleSeries, oldSeries);
        dbContext.SeriesMatches.Add(new SeriesMatch
        {
            Id = Guid.NewGuid(),
            SeriesId = staleSeries.Id,
            Name = "Stale Match",
            Format = MatchFormat.T20,
            StartTimeUtc = now.AddDays(3),
            Status = MatchStatus.Scheduled,
            StatusText = "Scheduled",
            SourceProvider = "CricketDataOrg",
            ExternalId = "stale-match",
            LastSyncedAtUtc = now.AddDays(-2)
        });
        await dbContext.SaveChangesAsync();

        var provider = new FakeSeriesProvider(
            providerName: "CricketDataOrg",
            upcomingSeries:
            [
                new ProviderSeries(
                    ExternalId: "active-series",
                    Name: "Active Series",
                    StartDateUtc: now.AddDays(5),
                    EndDateUtc: now.AddDays(15))
            ],
            detailsBySeriesId: new Dictionary<string, ProviderSeriesDetails?>
            {
                ["active-series"] = new ProviderSeriesDetails(
                    ExternalId: "active-series",
                    Name: "Active Series",
                    StartDateUtc: now.AddDays(5),
                    EndDateUtc: now.AddDays(15),
                    Matches:
                    [
                        new ProviderSeriesMatch(
                            ExternalId: "active-match-1",
                            Name: "Team A vs Team B",
                            VenueName: "Wankhede Stadium",
                            VenueCountry: "India",
                            Format: MatchFormat.ODI,
                            StartTimeUtc: now.AddDays(6),
                            Status: MatchStatus.Scheduled,
                            StatusText: "Scheduled")
                    ])
            });

        var syncService = CreateSyncService(
            dbContext,
            provider,
            new CricketProvidersOptions
            {
                Priority = ["CricketDataOrg"],
                SeriesSyncWindowDays = 30,
                SeriesInfoMaxRetries = 0
            });

        var result = await syncService.SyncUpcomingSeriesAsync();

        Assert.Equal("CricketDataOrg", result.ProviderUsed);
        Assert.Equal(1, result.SeriesUpserted);

        var seriesExternalIds = await dbContext.Series
            .AsNoTracking()
            .Where(x => x.SourceProvider == "CricketDataOrg")
            .Select(x => x.ExternalId)
            .ToListAsync();

        Assert.Contains("active-series", seriesExternalIds);
        Assert.Contains("old-series", seriesExternalIds);
        Assert.DoesNotContain("stale-series", seriesExternalIds);

        var remainingStaleMatches = await dbContext.SeriesMatches
            .AsNoTracking()
            .CountAsync(x => x.ExternalId == "stale-match");
        Assert.Equal(0, remainingStaleMatches);
    }

    [Fact]
    public async Task SyncUpcomingSeriesAsync_RetriesSeriesInfoAndSucceeds()
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        var provider = new FakeSeriesProvider(
            providerName: "CricketDataOrg",
            upcomingSeries:
            [
                new ProviderSeries(
                    ExternalId: "retry-series",
                    Name: "Retry Series",
                    StartDateUtc: now.AddDays(1),
                    EndDateUtc: now.AddDays(5))
            ],
            detailsBySeriesId: new Dictionary<string, ProviderSeriesDetails?>
            {
                ["retry-series"] = new ProviderSeriesDetails(
                    ExternalId: "retry-series",
                    Name: "Retry Series",
                    StartDateUtc: now.AddDays(1),
                    EndDateUtc: now.AddDays(5),
                    Matches:
                    [
                        new ProviderSeriesMatch(
                            ExternalId: "retry-match-1",
                            Name: "Retry A vs Retry B",
                            VenueName: "Eden Gardens",
                            VenueCountry: "India",
                            Format: MatchFormat.T20,
                            StartTimeUtc: now.AddDays(2),
                            Status: MatchStatus.Scheduled,
                            StatusText: "Scheduled")
                    ])
            },
            failuresBeforeSuccessBySeriesId: new Dictionary<string, int>
            {
                ["retry-series"] = 2
            });

        var syncService = CreateSyncService(
            dbContext,
            provider,
            new CricketProvidersOptions
            {
                Priority = ["CricketDataOrg"],
                SeriesSyncWindowDays = 30,
                SeriesInfoMaxRetries = 2,
                SeriesInfoRetryDelayMs = 1
            });

        var result = await syncService.SyncUpcomingSeriesAsync();

        Assert.Equal("CricketDataOrg", result.ProviderUsed);
        Assert.Equal(1, result.SeriesMatchesUpserted);
        Assert.Equal(3, provider.GetSeriesInfoCalls["retry-series"]);
    }

    private static SeriesSyncService CreateSyncService(
        CricStatsDbContext dbContext,
        ICricketProvider provider,
        CricketProvidersOptions options)
    {
        return new SeriesSyncService(
            dbContext,
            [provider],
            Options.Create(options),
            NullLogger<SeriesSyncService>.Instance,
            new FixedHostEnvironment("Development"));
    }

    private static CricStatsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CricStatsDbContext>()
            .UseInMemoryDatabase($"CricStatsSeriesUnitTests-{Guid.NewGuid()}")
            .Options;

        return new CricStatsDbContext(options);
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

    private sealed class FakeSeriesProvider : ICricketProvider
    {
        private readonly IReadOnlyList<ProviderSeries> _upcomingSeries;
        private readonly IReadOnlyDictionary<string, ProviderSeriesDetails?> _detailsBySeriesId;
        private readonly Dictionary<string, int> _failuresBeforeSuccessBySeriesId;

        public FakeSeriesProvider(
            string providerName,
            IReadOnlyList<ProviderSeries> upcomingSeries,
            IReadOnlyDictionary<string, ProviderSeriesDetails?> detailsBySeriesId,
            Dictionary<string, int>? failuresBeforeSuccessBySeriesId = null)
        {
            Name = providerName;
            _upcomingSeries = upcomingSeries;
            _detailsBySeriesId = detailsBySeriesId;
            _failuresBeforeSuccessBySeriesId = failuresBeforeSuccessBySeriesId ?? [];
        }

        public string Name { get; }

        public Dictionary<string, int> GetSeriesInfoCalls { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProviderUpcomingMatch>>([]);
        }

        public Task<IReadOnlyList<ProviderSeries>> GetUpcomingSeriesAsync(
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            var filtered = _upcomingSeries
                .Where(x =>
                {
                    var anchor = x.StartDateUtc ?? x.EndDateUtc ?? DateTimeOffset.MinValue;
                    return anchor >= fromUtc && anchor <= toUtc;
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<ProviderSeries>>(filtered);
        }

        public Task<ProviderSeriesDetails?> GetSeriesInfoAsync(
            string seriesExternalId,
            CancellationToken cancellationToken = default)
        {
            GetSeriesInfoCalls.TryGetValue(seriesExternalId, out var currentCalls);
            currentCalls++;
            GetSeriesInfoCalls[seriesExternalId] = currentCalls;

            if (_failuresBeforeSuccessBySeriesId.TryGetValue(seriesExternalId, out var failCount) &&
                currentCalls <= failCount)
            {
                throw new InvalidOperationException($"Synthetic failure for {seriesExternalId}.");
            }

            _detailsBySeriesId.TryGetValue(seriesExternalId, out var details);
            return Task.FromResult(details);
        }
    }
}
