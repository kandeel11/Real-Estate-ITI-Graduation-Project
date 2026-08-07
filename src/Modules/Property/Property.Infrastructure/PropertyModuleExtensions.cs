using AI.Application.Common.Options;
using AI.Application.Interfaces;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Properties.Infrastructure.Services;
using Property.Application.Features.Commands.CreateProperty;
using Property.Application.Interfaces;
using Property.Infrastructure.Persistence.Contexts;
using Property.Infrastructure.Persistence.Interceptors;
using Property.Infrastructure.Persistence.Repositories;
using Property.Infrastructure.Services;
using RealEstate.Shared.Kernel.Analytics;

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
        // Central domain-event dispatcher (publishes entity DomainEvents after SaveChanges)
        services.AddScoped<DispatchDomainEventsInterceptor>();

        services.AddDbContext<PropertyDbContext>((sp, options) =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(PropertyDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ISavedPropertyRepository, SavedPropertyRepository>();
        services.AddScoped<IPropertyAnalyticsRepository, PropertyAnalyticsRepository>();
        services.AddScoped<IPropertyAnalyticsService, PropertyAnalyticsService>();

        services.AddOptions<R2Options>().BindConfiguration(R2Options.SectionName);

        var r2Endpoint = configuration["R2:Endpoint"];
        if (!string.IsNullOrEmpty(r2Endpoint))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var r2 = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<R2Options>>().Value;
                return new AmazonS3Client(
                    new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey),
                    new AmazonS3Config
                    {
                        ServiceURL = r2.Endpoint,
                        ForcePathStyle = true,
                        AuthenticationRegion = r2.Region,
                        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
                    });
            });
            services.AddScoped<IMediaStorageService, R2MediaService>();
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
