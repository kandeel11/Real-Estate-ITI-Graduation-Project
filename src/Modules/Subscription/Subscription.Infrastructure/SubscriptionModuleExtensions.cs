using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Shared.Kernel.Analytics;
using RealEstate.Shared.Kernel.Shared.Contracts;
using Subscription.Application.Commands.CreatePlan;
using Subscription.Application.EventHandlers;
using Subscription.Application.Interfaces;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Persistence.Interceptors;
using Subscription.Infrastructure.Repositories;
using Subscription.Infrastructure.Services;

namespace Subscription.Infrastructure;

public static class SubscriptionModuleExtensions
{
    public static IServiceCollection AddSubscriptionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Central domain-event dispatcher (publishes entity DomainEvents after SaveChanges)
        services.AddScoped<DispatchDomainEventsInterceptor>();

        services.AddDbContext<SubscriptionDbContext>((sp, options) =>
            options
                .UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "subscription")
                              .MigrationsAssembly(typeof(SubscriptionDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePlanCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(IndividualUserCreatedHandler).Assembly);
        });

        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPaymentRepository, SubscriptionPaymentRepository>();
        services.AddScoped<ISubscriptionUsageRepository, SubscriptionUsageRepository>();

        services.AddScoped<ISubscriptionLimitChecker, SubscriptionLimitChecker>();
        services.AddScoped<ISubscriptionAnalyticsService, SubscriptionAnalyticsService>();

        services.AddValidatorsFromAssembly(typeof(CreatePlanCommand).Assembly);

        return services;
    }
}
