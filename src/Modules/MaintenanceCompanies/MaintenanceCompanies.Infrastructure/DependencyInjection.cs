using MaintenanceCompanies.Application.Interfaces;
using MaintenanceCompanies.Application.Services;
using MaintenanceCompanies.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceCompanies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMaintenanceCompaniesModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<MaintenanceModuleDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "maintenance")));

        services.AddScoped<IMaintenanceCompanyService, MaintenanceCompanyService>();
        services.AddScoped<ICompanyAssignmentService, CompanyAssignmentService>();
        services.AddScoped<IMaintenanceModuleDbContext, MaintenanceModuleDbContext>();

        return services;
    }
}