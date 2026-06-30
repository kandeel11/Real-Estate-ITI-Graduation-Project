using AI.Application;
using AI.Application.Interfaces;
using AI.Infrastructure;
using AI.Infrastructure.Services;
using AI.Infrastructure.Persistence.Contexts;
using Document.Infrastructure;
using Document.Infrastructure.Hubs;
using Document.Infrastructure.Persistence;
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
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RealEstate.Host.Adapters;
using RealEstate.Host.Behaviors;
using RealEstate.Host.Jobs;
using RealEstate.Host.Services;
using Subscription.Infrastructure;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Seed;
using Wallet.Infrastructure;
using Wallet.Infrastructure.Persistence;
using Azure.Storage.Blobs;
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
                // SignalR passes the JWT via the "access_token" query param for
                // WebSocket/SSE connections (the browser can't set headers there).
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"].ToString();
                        var path  = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(token) &&
                            (path.StartsWithSegments("/hubs/chat") ||
                             path.StartsWithSegments("/hubs/notifications")))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
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
        builder.Services.AddScoped<Property.Application.Interfaces.IAdminEmailService, PropertyAdminEmailAdapter>();
        builder.Services.AddScoped<Maintenance.Application.Interfaces.IPropertyManagementChecker, PropertyManagementCheckerAdapter>();
        builder.Services.AddScoped<Maintenance.Application.Interfaces.IMaintenancePaymentLinkService, MaintenancePaymentLinkAdapter>();
        builder.Services.AddScoped<Maintenance.Application.Interfaces.ILeaseDetailsAdapter, LeaseDetailsForDisputeAdapter>();
        builder.Services.AddScoped<Maintenance.Application.Interfaces.IWalletDeductionService, WalletDeductionAdapter>();

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

        // ── Redis ─────────────────────────────────────────────
        builder.Services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = builder.Configuration["Redis:ConnectionString"]);
        // Cache-aside pipeline behavior for all registered queries
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        builder.Services.AddScoped<PropertyEnrichmentService>();
        builder.Services.AddHostedService<LeaseExpiryBackgroundService>();
        builder.Services.AddAiApplication(builder.Configuration);
        builder.Services.AddAiInfrastructure(builder.Configuration);
        // Replace InMemorySessionStore with the Redis-backed implementation
        builder.Services.Replace(ServiceDescriptor.Singleton<ISessionStore, RedisSessionStore>());
        builder.Services.AddDocumentModule(builder.Configuration);
        builder.Services.AddMaintenanceCompaniesModule(builder.Configuration);

        // ── SignalR ───────────────────────────────────────────
        builder.Services.AddSignalR();


        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAngular", policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

        var app = builder.Build();

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

            var blobServiceClient = scope.ServiceProvider.GetService<BlobServiceClient>();
            var seedPublisher = scope.ServiceProvider.GetRequiredService<MediatR.IPublisher>();
            var seedSubscriptionRepo = scope.ServiceProvider
                .GetRequiredService<Subscription.Application.Interfaces.ISubscriptionRepository>();

            // Property demo seed — run once; skip entirely if data already present
            if (!await RealEstate.Host.Seeds.PropertyDataSeed.IsSeededAsync(identityDb))
            {
                await RealEstate.Host.Seeds.PropertyDataSeed.SeedAsync(
                    propertyDb, identityDb, hasher, blobServiceClient,
                    app.Environment.WebRootPath);
                await RealEstate.Host.Seeds.PropertyDataSeed.BackfillSubscriptionsAsync(
                    identityDb, seedSubscriptionRepo, seedPublisher);
            }

            // Qdrant vector seed — uploads seeded properties into the 'properties' collection
            // so they participate in duplicate detection from the first review submission.
            // Idempotent (skips IDs already in Qdrant) and non-blocking (wrapped in try-catch).
            await RealEstate.Host.Seeds.QdrantPropertySeed.SeedAsync(
                propertyDb,
                identityDb,
                scope.ServiceProvider.GetRequiredService<AI.Application.Interfaces.IPropertyDuplicateCheckService>(),
                scope.ServiceProvider.GetRequiredService<Qdrant.Client.QdrantClient>(),
                app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("QdrantPropertySeed"));

            var paymentDb = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            await paymentDb.Database.MigrateAsync();

            var walletDb = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            await walletDb.Database.MigrateAsync();

            var maintenanceCompaniesDb = scope.ServiceProvider.GetRequiredService<MaintenanceModuleDbContext>();
            await maintenanceCompaniesDb.Database.MigrateAsync();

            // Tenant + maintenance company seed — run once; skip entirely if data already present
            if (!await RealEstate.Host.Seeds.TenantAndMaintenanceCompanySeed.IsSeededAsync(identityDb))
            {
                await RealEstate.Host.Seeds.TenantAndMaintenanceCompanySeed.SeedAsync(
                    identityDb, maintenanceCompaniesDb, hasher);
                await RealEstate.Host.Seeds.TenantAndMaintenanceCompanySeed.BackfillTenantSubscriptionsAsync(
                    identityDb, seedSubscriptionRepo, seedPublisher);
            }
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
        app.MapHub<NotificationHub>("/hubs/notifications");
        app.Run();
    }
}