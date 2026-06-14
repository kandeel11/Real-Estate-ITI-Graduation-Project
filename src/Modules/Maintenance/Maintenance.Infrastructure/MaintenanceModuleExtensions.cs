using Maintenance.Application;
using Maintenance.Application.Interfaces;
using Maintenance.Infrastructure.Persistence.Contexts;
using Maintenance.Infrastructure.Persistence.Repositories;
using Maintenance.Infrastructure.Services;
using Maintenance.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.Infrastructure;

public static class MaintenanceModuleExtensions
{
    public static IServiceCollection AddMaintenanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<MaintenanceDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(MaintenanceDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<ITicketAiEvaluationRepository, TicketAiEvaluationRepository>();
        services.AddScoped<IVendorAssignmentRepository, VendorAssignmentRepository>();
        services.AddScoped<IMaintenancePaymentRepository, MaintenancePaymentRepository>();

        // Azure Blob Storage — reads ConnectionString from User Secrets,
        // ContainerName from appsettings.json
        services.Configure<AzureStorageSettings>(configuration.GetSection("AzureStorage"));
        services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();

        // Other services
        services.AddScoped<IAutoVendorAssignmentService, AutoVendorAssignmentService>();
        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
        services.AddHttpClient("AiEvaluation");
        services.AddScoped<IAiEvaluationService, AiEvaluationService>();

        // MediatR handlers
        services.AddMaintenanceApplication();

        return services;
    }
}