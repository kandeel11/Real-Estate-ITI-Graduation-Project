using AI.Application;
using AI.Infrastructure;
//using AI.Infrastructure.EventHandlers;
using Document.Infrastructure;
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
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName);
        });
        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddSubscriptionModule(builder.Configuration);
        // ==========================================
        // 2. Register MediatR (CQRS)
        // ==========================================
        builder.Services.AddMediatR(cfg =>
        {

            cfg.RegisterServicesFromAssemblies(
                typeof(AI.Application.AssemblyMarker).Assembly,
                typeof(AI.Infrastructure.AssemblyMarker).Assembly,
                typeof(Property.Infrastructure.AssemblyMarker).Assembly,
                typeof(Document.Infrastructure.AssemblyMarker).Assembly,
                typeof(Document.Application.AssemblyMarker).Assembly
                //,typeof(LeaseUploadedEventHandler).Assembly

            );
        });

        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddAiApplication(builder.Configuration);
        builder.Services.AddAiInfrastructure(builder.Configuration);
        builder.Services.AddDocumentModule(builder.Configuration);

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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate API v1");
            });
        }
        // Configure the HTTP request pipeline.
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}