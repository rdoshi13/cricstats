using CricStats.Application.Models.Providers;

namespace CricStats.Application.Interfaces.Providers;

public interface ICricketProvider
{
    string Name { get; }

    Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProviderSeries>> GetUpcomingSeriesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ProviderSeries>>([]);
    }

    Task<ProviderSeriesDetails?> GetSeriesInfoAsync(
        string seriesExternalId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ProviderSeriesDetails?>(null);
    }
}
