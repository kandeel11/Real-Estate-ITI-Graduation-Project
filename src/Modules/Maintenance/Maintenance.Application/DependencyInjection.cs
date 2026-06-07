using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMaintenanceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}