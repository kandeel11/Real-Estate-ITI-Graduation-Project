using System.Threading.Channels;
using FluentValidation;
using Identity.Application.Commands.RegisterIndividualUser;
using Identity.Application.Features.Auth.Otp.SendOtp;
using Identity.Application.Interfaces;
using Identity.Application.Interfaces.Common;
using Identity.Infrastructure.Email;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.Shared.Kernel.Analytics;

namespace Identity.Infrastructure;

public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")
                          .MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName)));

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        

        services.AddValidatorsFromAssembly(typeof(SendOtpHandler).Assembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RegisterIndividualUserCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(SendOtpHandler).Assembly);
    });
        services.AddScoped<IUserAnalyticsService, UserAnalyticsService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IBrokerRepository, BrokerRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMaintenanceCompanyRepository, MaintenanceCompanyRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssembly(typeof(RegisterIndividualUserCommand).Assembly);
        services.AddMemoryCache();

        // Email config
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Core services
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IEmailTemplateRenderer, ScribanEmailTemplateRenderer>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();

        // Scoped because it uses the DbContext through IUserRepository
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IEmailJobs, EmailJobs>();

        // In-memory email queue — bounded at 500; drops with a warning if full (fire-and-forget emails)
        var emailChannel = Channel.CreateBounded<EmailJobWorkItem>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
        services.AddSingleton(emailChannel);
        services.AddSingleton<IEmailJobScheduler, ChannelEmailJobScheduler>();
        services.AddHostedService<EmailBackgroundWorker>();


        return services;
    }
}
