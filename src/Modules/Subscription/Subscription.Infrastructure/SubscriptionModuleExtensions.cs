using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.Contracts;
using Subscription.Application.Commands.CreatePlan;
using Subscription.Application.EventHandlers;
using Subscription.Application.Interfaces;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Repositories;
using Subscription.Infrastructure.Services;

namespace Subscription.Infrastructure;

public static class SubscriptionModuleExtensions
{
    public static IServiceCollection AddSubscriptionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SubscriptionDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "subscription")
                          .MigrationsAssembly(typeof(SubscriptionDbContext).Assembly.FullName)));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreatePlanCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(IndividualUserCreatedHandler).Assembly);
            // SubscriptionPaymentSucceededEventHandler موجود في نفس الـ assembly،
            // فبيتسجل أوتوماتيك مع الـ scan ده.
        });

        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionUsageRepository, SubscriptionUsageRepository>();

        services.AddScoped<ISubscriptionLimitChecker, SubscriptionLimitChecker>();

        // ── Adapter Pattern ──────────────────────────────────────────
        // Payment.Application بيعرّف ISubscriptionPlanService، وهنا بنربطه
        // بالـ implementation الحقيقي اللي بيقرأ من SubscriptionDbContext.
        // ده اللي يحل مشكلة PlanNotFoundException القديمة (MockSubscriptionPlanService
        // كان بيرجع IDs ثابتة غير موجودة في الداتابيز).
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();

        services.AddValidatorsFromAssembly(typeof(CreatePlanCommand).Assembly);

        return services;
    }
}
