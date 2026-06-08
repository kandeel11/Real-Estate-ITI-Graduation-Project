using AI.Application.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Properties.Infrastructure.Services;
using Property.Application.Features.Commands.CreateProperty;
using Property.Application.Interfaces;
using Property.Infrastructure.Persistence.Contexts;
using Property.Infrastructure.Persistence.Repositories;
using Property.Infrastructure.Services;

namespace Property.Infrastructure;

public static class PropertyModuleExtensions
{
    public static IServiceCollection AddPropertyModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Properties.Infrastructure — register PropertyFilterService
        services.AddScoped<
            AI.Application.Interfaces.IPropertyFilterService,
            PropertyFilterService>();
        services.AddDbContext<PropertyDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(PropertyDbContext).Assembly.FullName)));

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IPropertyAnalyticsRepository, PropertyAnalyticsRepository>();

        var azureBlobConnectionString = configuration.GetConnectionString("AzureBlobStorage");
        if (!string.IsNullOrEmpty(azureBlobConnectionString))
        {
            services.AddSingleton(new BlobServiceClient(azureBlobConnectionString));
            services.AddScoped<IMediaStorageService, AzureBlobMediaService>();
        }
        else
        {
            services.AddScoped<IMediaStorageService, NullMediaStorageService>();
        }

        // Register MediatR handlers from Property.Application assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreatePropertyCommand).Assembly));

        return services;
    }
}
