using AI.Application;
using AI.Infrastructure;
using AI.Infrastructure.Persistence.Contexts;
using Document.Infrastructure;
using Document.Infrastructure.Hubs;
using Document.Infrastructure.Persistence;
using Hangfire;
using Hangfire.SqlServer;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Seed;
using Identity.Infrastructure.Services;
using Maintenance.Infrastructure;
using Maintenance.Infrastructure.Persistence.Contexts;
using MaintenanceCompanies.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;
using Property.Infrastructure;
using Property.Infrastructure.Persistence.Contexts;
using Payment.Application.Interfaces;
using RealEstate.Host.Adapters;
using RealEstate.Host.Infrastructure;
using RealEstate.Host.Jobs;
using RealEstate.Host.Services;
using Subscription.Infrastructure;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Seed;
using Wallet.Infrastructure;
using Wallet.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MaintenanceCompanies.Infrastructure;
using MaintenanceCompanies.Application;
using Document.Infrastructure.Persistence;
using MaintenanceCompanies.Infrastructure.Persistence;
using Maintenance.Infrastructure.Persistence.Contexts;

namespace RealEstate.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddHttpContextAccessor();

        // Allow the Angular dev server to call the API from the browser
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy => policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName);
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter your JWT token (without 'Bearer ' prefix)"
            });
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ── JWT ───────────────────────────────────────────────
        var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? new JwtOptions { SigningKey = "default-dev-key-change-in-production-32chars" };

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // ── Authorization Policies ────────────────────────────
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
            options.AddPolicy("OwnerOrAdmin", p => p.RequireRole("owner", "broker", "company_manager", "admin"));
            options.AddPolicy("BrokerOrCompanyManager", p => p.RequireRole("broker", "company_manager", "admin"));
            options.AddPolicy("TenantOnly", p => p.RequireRole("tenant"));
            options.AddPolicy("CompanyManager", p => p.RequireRole("company_manager", "admin"));
            options.AddPolicy("MaintenanceCompany", p => p.RequireRole("maintenance_company"));
            options.AddPolicy("InternalServiceOnly", p => p.RequireClaim("service_type"));
        });

        // ── Modules ───────────────────────────────────────────
        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddMaintenanceModule(builder.Configuration);
        builder.Services.AddSubscriptionModule(builder.Configuration);

        // Real adapters registered before AddPaymentModule so its TryAddScoped mocks are never used.
        builder.Services.AddScoped<ILeaseService, DocumentLeaseServiceAdapter>();
        builder.Services.AddScoped<INotificationService, SmtpNotificationServiceAdapter>();

        builder.Services.AddPaymentModule(builder.Configuration);
        builder.Services.AddWalletModule(builder.Configuration);

        // ── MediatR ───────────────────────────────────────────
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(AI.Application.AssemblyMarker).Assembly,
                typeof(AI.Infrastructure.AssemblyMarker).Assembly,
                typeof(Identity.Application.AssemblyMarker).Assembly,
                typeof(Identity.Infrastructure.AssemblyMarker).Assembly,
                typeof(Property.Infrastructure.AssemblyMarker).Assembly,
                typeof(Document.Infrastructure.AssemblyMarker).Assembly,
                typeof(Document.Application.AssemblyMarker).Assembly,
                typeof(MaintenanceCompanies.Infrastructure.AssemblyReference).Assembly,
                typeof(MaintenanceCompanies.Application.AssemblyReference).Assembly
            );
        });

        builder.Services.AddScoped<PropertyEnrichmentService>();
        builder.Services.AddScoped<LeaseExpiryJob>();
        builder.Services.AddHostedService<LeaseExpiryBackgroundService>();
        builder.Services.AddAiApplication(builder.Configuration);
        builder.Services.AddAiInfrastructure(builder.Configuration);
        builder.Services.AddDocumentModule(builder.Configuration);
        builder.Services.AddMaintenanceCompaniesModule(builder.Configuration);

        // ── SignalR ───────────────────────────────────────────
        builder.Services.AddSignalR();

        // ── Hangfire (critical scheduled jobs only) ───────────
        var hangfireOpts = builder.Configuration.GetSection("Hangfire").Get<HangfireOptions>()
            ?? new HangfireOptions();

        if (hangfireOpts.Enabled)
        {
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromSeconds(hangfireOpts.QueuePollIntervalSeconds),
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

            builder.Services.AddHangfireServer(options =>
            {
                options.WorkerCount = hangfireOpts.WorkerCount;
                options.Queues = new[] { "default" };
            });
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        var app = builder.Build();

        if (hangfireOpts.Enabled)
        {
            if (hangfireOpts.DashboardEnabled)
            {
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new[] { new HangfireDashboardAuthFilter() }
                });
            }

            // Daily sweep: end leases past their EndDate and return properties to market.
            RecurringJob.AddOrUpdate<LeaseExpiryJob>(
                "expire-due-leases",
                job => job.RunAsync(),
                Cron.Daily);
        }

        // ── Migrations + Seeding ──────────────────────────────
        using (var scope = app.Services.CreateScope())
        {
            var aiDb = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            var propertyDb = scope.ServiceProvider.GetRequiredService<PropertyDbContext>();
            var maintenanceDb = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
            var documentDb = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var subscriptionDb = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();

            await documentDb.Database.MigrateAsync();
            await maintenanceDb.Database.MigrateAsync();
            await propertyDb.Database.MigrateAsync();
            await aiDb.Database.MigrateAsync();
            await subscriptionDb.Database.MigrateAsync();

            await SubscriptionPlanSeed.SeedAsync(subscriptionDb);
            await Property.Infrastructure.Persistence.Seeding.AmenitySeed.SeedAsync(propertyDb);

            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await identityDb.Database.MigrateAsync();
            var hasher = scope.ServiceProvider.GetRequiredService<Identity.Application.Interfaces.IPasswordHasher>();
            await AdminSeed.SeedAsync(identityDb, hasher);

            var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            await paymentDb.Database.MigrateAsync();

            var walletDb = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            await walletDb.Database.MigrateAsync();

            var maintenanceCompaniesDb = scope.ServiceProvider.GetRequiredService<MaintenanceModuleDbContext>();
            await maintenanceCompaniesDb.Database.MigrateAsync();
        }
        //stripe
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/stripe/webhook"))
            {
                context.Request.EnableBuffering();
            }
            await next();
        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate API v1"));
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors("AllowAngular");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<DocumentChatHub>("/hubs/chat");
        app.Run();
    }
}