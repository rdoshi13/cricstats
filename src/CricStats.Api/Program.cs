using CricStats.Application;
using CricStats.Application.Interfaces;
using CricStats.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);
var hangfireEnabled = builder.Configuration.GetValue<bool?>("Hangfire:Enabled")
    ?? !builder.Environment.IsEnvironment("Testing");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

if (hangfireEnabled)
{
    var connectionString = builder.Configuration.GetConnectionString("CricStatsDb");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'CricStatsDb' is required when Hangfire is enabled.");
    }

    builder.Services.AddHangfire(config =>
    {
        config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString));
    });

    builder.Services.AddHangfireServer();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (hangfireEnabled)
{
    if (app.Environment.IsDevelopment())
    {
        var dashboardPath = builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire";
        app.UseHangfireDashboard(dashboardPath);
    }

    var syncUpcomingCron = builder.Configuration["Hangfire:Jobs:SyncUpcomingMatchesCron"] ?? "0 */6 * * *";
    var refreshWeatherCron = builder.Configuration["Hangfire:Jobs:RefreshWeatherRiskCron"] ?? "0 */3 * * *";
    var syncSeriesCron = builder.Configuration["Hangfire:Jobs:SyncUpcomingSeriesCron"] ?? "0 0 * * *";
    using var scope = app.Services.CreateScope();
    var recurringJobs = scope.ServiceProvider.GetService<IRecurringJobManager>();
    if (recurringJobs is not null)
    {
        recurringJobs.AddOrUpdate<IUpcomingMatchesSyncService>(
            "sync-upcoming-matches",
            service => service.SyncUpcomingMatchesAsync(CancellationToken.None),
            syncUpcomingCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        recurringJobs.AddOrUpdate<IWeatherRiskService>(
            "refresh-weather-risk",
            service => service.RefreshUpcomingWeatherRiskAsync(CancellationToken.None),
            refreshWeatherCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        recurringJobs.AddOrUpdate<ISeriesSyncService>(
            "sync-upcoming-series",
            service => service.SyncUpcomingSeriesAsync(CancellationToken.None),
            syncSeriesCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}

app.MapControllers();

app.Run();

public partial class Program;
