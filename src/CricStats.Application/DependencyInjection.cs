using Microsoft.Extensions.DependencyInjection;

namespace CricStats.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application service registrations will expand as use-cases are introduced.
        return services;
    }
}
