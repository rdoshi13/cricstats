using CricStats.Application.Models.Weather;
using CricStats.Contracts.Weather;

namespace CricStats.Application.Interfaces;

public interface IWeatherRiskService
{
    Task<RefreshWeatherRiskResult> RefreshUpcomingWeatherRiskAsync(
        CancellationToken cancellationToken = default);

    Task<MatchWeatherRiskResponse?> GetMatchWeatherRiskAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);
}
