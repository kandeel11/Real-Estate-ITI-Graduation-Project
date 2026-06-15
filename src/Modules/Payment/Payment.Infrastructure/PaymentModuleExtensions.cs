using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application;
using Payment.Application.Interfaces;
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

        services.AddScoped<IRentPaymentRepository, RentPaymentRepository>();
        services.AddScoped<IPaymentAnalyticsService, PaymentAnalyticsService>();

        services.Configure<StripeSettings>(
            configuration.GetSection(StripeSettings.SectionName));

        services.AddScoped<StripePaymentGatewayService>();
        services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();

        services.AddScoped<StripeWebhookService>();

        services.AddScoped<ILeaseService, MockLeaseService>();

        services.AddScoped<INotificationService, MockNotificationService>();

        return services;
    }
}
