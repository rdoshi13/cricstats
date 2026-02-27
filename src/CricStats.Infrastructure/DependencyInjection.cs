using CricStats.Application.Interfaces;
using CricStats.Application.Interfaces.Providers;
using CricStats.Infrastructure.Persistence;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Providers;
using CricStats.Infrastructure.Providers.Weather;
using CricStats.Infrastructure.Services;
using CricStats.Infrastructure.Services.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CricStats.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CricStatsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CricStatsDb' is not configured.");
        }

        var providerSection = configuration.GetSection(CricketProvidersOptions.SectionName);
        var priority = providerSection.GetSection("Priority")
            .GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

        var syncWindowDays = int.TryParse(providerSection["SyncWindowDays"], out var parsedSyncWindow)
            ? parsedSyncWindow
            : 14;

        services.AddSingleton<IOptions<CricketProvidersOptions>>(Microsoft.Extensions.Options.Options.Create(new CricketProvidersOptions
        {
            Priority = priority,
            SyncWindowDays = syncWindowDays
        }));

        var liveCricketSection = configuration.GetSection(LiveCricketOptions.SectionName);
        var liveCricketEnabled = bool.TryParse(liveCricketSection["Enabled"], out var parsedLiveEnabled)
            ? parsedLiveEnabled
            : true;
        var liveCricketBaseUrl = liveCricketSection["BaseUrl"] ?? "https://cricbuzz-live.vercel.app";
        var liveCricketMatchType = liveCricketSection["MatchType"] ?? "international";
        var liveCricketTimeoutSeconds = int.TryParse(liveCricketSection["TimeoutSeconds"], out var parsedLiveTimeout)
            ? parsedLiveTimeout
            : 8;

        services.AddSingleton<IOptions<LiveCricketOptions>>(Microsoft.Extensions.Options.Options.Create(new LiveCricketOptions
        {
            Enabled = liveCricketEnabled,
            BaseUrl = liveCricketBaseUrl,
            MatchType = liveCricketMatchType,
            TimeoutSeconds = liveCricketTimeoutSeconds
        }));

        var cricketDataSection = configuration.GetSection(CricketDataOrgApiOptions.SectionName);
        var cricketDataEnabled = bool.TryParse(cricketDataSection["Enabled"], out var parsedCricketDataEnabled)
            ? parsedCricketDataEnabled
            : true;
        var cricketDataBaseUrl = cricketDataSection["BaseUrl"] ?? "https://api.cricapi.com";
        var cricketDataApiKey = cricketDataSection["ApiKey"] ?? string.Empty;
        var cricketDataTimeoutSeconds = int.TryParse(cricketDataSection["TimeoutSeconds"], out var parsedCricketDataTimeout)
            ? parsedCricketDataTimeout
            : 8;

        services.AddSingleton<IOptions<CricketDataOrgApiOptions>>(Microsoft.Extensions.Options.Options.Create(new CricketDataOrgApiOptions
        {
            Enabled = cricketDataEnabled,
            BaseUrl = cricketDataBaseUrl,
            ApiKey = cricketDataApiKey,
            TimeoutSeconds = cricketDataTimeoutSeconds
        }));

        var weatherSection = configuration.GetSection(WeatherRiskOptions.SectionName);
        var providerName = weatherSection["ProviderName"] ?? "OpenMeteoStub";
        var refreshWindowDays = int.TryParse(weatherSection["RefreshWindowDays"], out var parsedRefreshWindowDays)
            ? parsedRefreshWindowDays
            : 14;
        var precipAmountMaxMm = decimal.TryParse(weatherSection["PrecipAmountMaxMm"], out var parsedPrecipAmountMax)
            ? parsedPrecipAmountMax
            : 20m;
        var windSpeedMaxKph = decimal.TryParse(weatherSection["WindSpeedMaxKph"], out var parsedWindSpeedMax)
            ? parsedWindSpeedMax
            : 60m;

        services.AddSingleton<IOptions<WeatherRiskOptions>>(Microsoft.Extensions.Options.Options.Create(new WeatherRiskOptions
        {
            ProviderName = providerName,
            RefreshWindowDays = refreshWindowDays,
            PrecipAmountMaxMm = precipAmountMaxMm,
            WindSpeedMaxKph = windSpeedMaxKph
        }));

        services.AddDbContext<CricStatsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpClient("CricbuzzLive", client =>
        {
            client.BaseAddress = new Uri(liveCricketBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(liveCricketTimeoutSeconds, 2, 30));
        });
        services.AddHttpClient("CricketDataOrg", client =>
        {
            client.BaseAddress = new Uri(cricketDataBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(cricketDataTimeoutSeconds, 2, 30));
        });
        services.AddHttpClient("OpenMeteoGeocoding", client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient("OpenMeteoWeather", client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ICricketProvider, CricbuzzLiveProvider>();
        services.AddScoped<ICricketProvider, CricketDataOrgProvider>();
        services.AddScoped<IWeatherProvider, OpenMeteoStubProvider>();
        services.AddScoped<IUpcomingMatchesSyncService, UpcomingMatchesSyncService>();
        services.AddScoped<IUpcomingMatchesService, UpcomingMatchesService>();
        services.AddScoped<IWeatherRiskService, WeatherRiskService>();

        return services;
    }
}
