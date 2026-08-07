using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Application.Commands.RequestWithdrawal;
using Wallet.Application.Interfaces;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Repositories;

namespace Wallet.Infrastructure;

public static class WalletModuleExtensions
{
    public static IServiceCollection AddWalletModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WalletDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "wallet")
                          .MigrationsAssembly(typeof(WalletDbContext).Assembly.FullName)));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RequestWithdrawalCommand).Assembly));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();

        return services;
    }
}
