using AI.Application;
using AI.Infrastructure;
using AI.Infrastructure.Persistence.Contexts;
using Document.Infrastructure;
using Document.Infrastructure.Hubs;
using Hangfire;
using Hangfire.SqlServer;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Seed;
using Identity.Infrastructure.Services;
using Maintenance.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Property.Infrastructure;
using Property.Infrastructure.Persistence.Contexts;
using RealEstate.Host.Services;
using Subscription.Infrastructure;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Seed;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MaintenanceCompanies.Infrastructure;
using MaintenanceCompanies.Application;

namespace RealEstate.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
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

        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddMaintenanceModule(builder.Configuration);
        builder.Services.AddSubscriptionModule(builder.Configuration);

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
        builder.Services.AddAiApplication(builder.Configuration);
        builder.Services.AddAiInfrastructure(builder.Configuration);
        builder.Services.AddDocumentModule(builder.Configuration);
        builder.Services.AddMaintenanceCompaniesModule(builder.Configuration);

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
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "emails", "default" };
        });

        var app = builder.Build();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthFilter() }
        });

        using (var scope = app.Services.CreateScope())
        {
            var subscriptionDb = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
            var AiDB = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            var PropertyDb = scope.ServiceProvider.GetRequiredService<PropertyDbContext>();
            await PropertyDb.Database.MigrateAsync();
            await AiDB.Database.MigrateAsync();
            await subscriptionDb.Database.MigrateAsync();
            await SubscriptionPlanSeed.SeedAsync(subscriptionDb);

            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await identityDb.Database.MigrateAsync();
            var hasher = scope.ServiceProvider.GetRequiredService<Identity.Application.Interfaces.IPasswordHasher>();
            await AdminSeed.SeedAsync(identityDb, hasher);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate API v1");
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<DocumentChatHub>("/hubs/chat");
        app.Run();
    }
}