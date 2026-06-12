using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure;

public static class PaymentModuleExtensions
{
    public static IServiceCollection AddPaymentModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(PaymentDbContext).Assembly.FullName)));

        services.AddScoped<IPaymentDbContext>(sp =>
            sp.GetRequiredService<PaymentDbContext>());

        return services;
    }
}