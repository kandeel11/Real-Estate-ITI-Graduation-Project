using Document.Application.Interfaces;
using Document.Infrastructure.Persistence;
using Document.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Document.Infrastructure;

public static class DocumentModule
{
    public static IServiceCollection AddDocumentModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<DocumentDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Register interfaces
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<DocumentDbContext>());
        services.AddScoped<IAzureBlobService, AzureBlobService>();

        return services;
    }
}