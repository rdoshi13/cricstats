using CricStats.Application.Interfaces;
using CricStats.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CricStats.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUpcomingMatchesService, UpcomingMatchesService>();
        return services;
    }
}
