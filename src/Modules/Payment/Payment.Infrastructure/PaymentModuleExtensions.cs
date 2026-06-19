using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payment.Application;
using Payment.Application.Interfaces;
using Payment.Infrastructure.BackgroundJobs;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Services;
using Payment.Infrastructure.Services.Stripe;
using Payment.Infrastructure.Settings;
using RealEstate.Shared.Kernel.Analytics;

namespace Payment.Infrastructure;

public static class PaymentModuleExtensions
{
    public static IServiceCollection AddPaymentModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPaymentApplication();

        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "payment")));

        services.AddScoped<IPaymentDbContext>(sp =>
            sp.GetRequiredService<PaymentDbContext>());

        //services.AddScoped<IRentPaymentRepository, RentPaymentRepository>();
        services.AddScoped<IPaymentAnalyticsService, PaymentAnalyticsService>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();


        services.Configure<StripeSettings>(
            configuration.GetSection(StripeSettings.SectionName));

        services.AddScoped<StripePaymentGatewayService>();
        services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();

        services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();

        services.AddScoped<StripeWebhookService>();

        services.AddSingleton<OutboxProcessingSignal>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddHostedService<OutboxPublisherJob>();

        services.Configure<OverdueJobSettings>(
            configuration.GetSection(OverdueJobSettings.SectionName));
        services.AddHostedService<OverduePaymentsJob>();
        services.AddHostedService<MonthlyPaymentGenerationJob>();

        services.TryAddScoped<INotificationService, MockNotificationService>();

        // ISubscriptionPlanService مش بيتسجل هنا — التسجيل الحقيقي موجود في
        // Subscription.Infrastructure.SubscriptionModuleExtensions (Adapter Pattern)
        // عشان Payment.Infrastructure لا يعرف أي شيء عن Subscription.Infrastructure
        // (Modular Monolith boundary). تأكد من استدعاء AddSubscriptionModule()
        // في Program.cs حتى يتوفر هذا الـ registration في الـ DI container.

        return services;
    }
}
