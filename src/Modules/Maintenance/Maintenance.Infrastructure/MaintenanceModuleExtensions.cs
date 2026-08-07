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
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(MaintenanceDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<ITicketAiEvaluationRepository, TicketAiEvaluationRepository>();
        services.AddScoped<IVendorAssignmentRepository, VendorAssignmentRepository>();
        services.AddScoped<IMaintenancePaymentRepository, MaintenancePaymentRepository>();

        // R2 (Cloudflare) object storage — credentials from User Secrets / env vars
        services.Configure<R2StorageSettings>(configuration.GetSection("R2"));
        services.AddScoped<IFileStorageService, R2FileStorageService>();

        // Other services
        services.AddScoped<IAutoVendorAssignmentService, AutoVendorAssignmentService>();
        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();

        // AI Agent — ITI Student API (DeepSeek v3.2)
        // Singletons: Qdrant/OpenAI clients are thread-safe and expensive to create.
        services.AddHttpClient("ITIStudentApi", (sp, client) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var apiKey = cfg["ITIStudentApi:ApiKey"]
                         ?? throw new InvalidOperationException(
                             "Secret 'ITIStudentApi:ApiKey' is not configured.");
            client.BaseAddress = new Uri("http://apiaccess.iti.net.eg/api/v1/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        });
        services.AddSingleton<IContractClauseSearchService, QdrantContractSearchService>();
        services.AddSingleton<IAiEvaluationService, StudentApiMaintenanceAgent>();

        // MediatR handlers
        services.AddMaintenanceApplication();

        return services;
    }
}