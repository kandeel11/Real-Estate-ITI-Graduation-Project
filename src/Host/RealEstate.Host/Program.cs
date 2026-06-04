using AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription;
using AI.Infrastructure;
using Property.Infrastructure;
using Subscription.Infrastructure;
using Subscription.Infrastructure.Persistence;
using Subscription.Infrastructure.Seed;

namespace RealEstate.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddMediatR(cfg =>
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic &&
                            a.GetName().Name != null &&
                            !a.GetName().Name!.StartsWith("Microsoft") &&
                            !a.GetName().Name!.StartsWith("System"))
                .ToArray();

            cfg.RegisterServicesFromAssemblies(loadedAssemblies);
        });
        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddSubscriptionModule(builder.Configuration);
        // ==========================================
        // 2. Register MediatR (CQRS)
        // ==========================================
        builder.Services.AddMediatR(cfg =>
        {          
            cfg.RegisterServicesFromAssemblyContaining<GeneratePropertyDescriptionCommand>();
        });

        builder.Services.AddAiInfrastructure(builder.Configuration);
        // Add services to the container.
        builder.Services.AddSwaggerGen();
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var subscriptionDb = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
            await SubscriptionPlanSeed.SeedAsync(subscriptionDb);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate Management API v1");
            });
        }
        // Configure the HTTP request pipeline.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }
}

