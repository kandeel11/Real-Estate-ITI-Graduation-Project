namespace Maintenance.Infrastructure;

using global::Maintenance.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


public static class MaintenanceModuleExtensions
{
    public static IServiceCollection AddMaintenanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MaintenanceDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(MaintenanceDbContext).Assembly.FullName)));

        return services;
    }
}
