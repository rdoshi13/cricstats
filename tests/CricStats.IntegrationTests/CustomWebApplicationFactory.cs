using CricStats.Application.Interfaces.Providers;
using CricStats.Infrastructure.Persistence;
using CricStats.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CricStats.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"CricStatsIntegration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CricStatsDbContext>>();
            services.RemoveAll<CricStatsDbContext>();
            services.RemoveAll<IOptions<CricketProvidersOptions>>();
            services.RemoveAll<IOptions<CricketDataOrgApiOptions>>();
            services.RemoveAll<IOptions<LiveCricketOptions>>();
            services.RemoveAll<ICricketProvider>();
            services.RemoveAll<IWeatherProvider>();

            services.AddDbContext<CricStatsDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddSingleton<IOptions<CricketProvidersOptions>>(Options.Create(new CricketProvidersOptions
            {
                Priority = ["TestCricket"],
                SyncWindowDays = 14
            }));

            services.AddSingleton<IOptions<CricketDataOrgApiOptions>>(Options.Create(new CricketDataOrgApiOptions
            {
                Enabled = false,
                BaseUrl = "https://api.cricapi.com",
                ApiKey = string.Empty,
                TimeoutSeconds = 8
            }));

            services.AddSingleton<IOptions<LiveCricketOptions>>(Options.Create(new LiveCricketOptions
            {
                Enabled = false,
                BaseUrl = "https://cricbuzz-live.vercel.app",
                MatchType = "international",
                TimeoutSeconds = 8
            }));

            services.AddScoped<ICricketProvider, TestCricketProvider>();
            services.AddScoped<IWeatherProvider, TestWeatherProvider>();
        });
    }
}
