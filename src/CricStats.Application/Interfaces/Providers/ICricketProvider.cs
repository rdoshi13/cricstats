using CricStats.Application.Models.Providers;

namespace CricStats.Application.Interfaces.Providers;

public interface ICricketProvider
{
    string Name { get; }

    Task<IReadOnlyList<ProviderUpcomingMatch>> GetUpcomingMatchesAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
