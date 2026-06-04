using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var zillowBaseUrl = configuration["Zillow:BaseUrl"];
        if (!string.IsNullOrEmpty(zillowBaseUrl))
        {
            services.AddHttpClient<IPropertySyndicationService, ZillowSyndicationClient>(client =>
            {
                client.BaseAddress = new Uri(zillowBaseUrl);
                var apiKey = configuration["Zillow:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            });
        }
        else
        {
            services.AddScoped<IPropertySyndicationService, NullPropertySyndicationService>();
        }

        // Register MediatR handlers from Property.Application assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreatePropertyCommand).Assembly));

        return services;
    }
}
