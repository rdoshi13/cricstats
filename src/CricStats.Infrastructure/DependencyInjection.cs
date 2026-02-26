using CricStats.Application.Interfaces;
using CricStats.Application.Interfaces.Providers;
using CricStats.Infrastructure.Persistence;
using CricStats.Infrastructure.Options;
using CricStats.Infrastructure.Providers;
using CricStats.Infrastructure.Services;
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

        services.AddDbContext<CricStatsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICricketProvider, CricketDataOrgProvider>();
        services.AddScoped<ICricketProvider, ApiSportsProvider>();
        services.AddScoped<IUpcomingMatchesSyncService, UpcomingMatchesSyncService>();
        services.AddScoped<IUpcomingMatchesService, UpcomingMatchesService>();

        return services;
    }
}
