using AI.Application;
using AI.Infrastructure;
using Property.Infrastructure;

namespace RealEstate.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Controllers & Swagger ─────────────────────────────
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName);
        });

         //── MediatR — ONE registration, scan all your assemblies ──
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(AI.Application.AssemblyMarker).Assembly,     // AI.Application
                typeof(AI.Infrastructure.AssemblyMarker).Assembly,  // AI.Infrastructure
                typeof(Property.Infrastructure.AssemblyMarker).Assembly // Property
                                                                        // add more assemblies here as you add modules
            );
        });

        //// ── Module registrations ──────────────────────────────
        builder.Services.AddPropertyModule(builder.Configuration);
        builder.Services.AddAiApplication(builder.Configuration);
        builder.Services.AddAiInfrastructure(builder.Configuration);

        // ── Build ─────────────────────────────────────────────
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate API v1");
            });
        }

        // ── Middleware pipeline (ORDER MATTERS) ───────────────
        app.UseHttpsRedirection();
        app.UseRouting();           // ← THIS WAS MISSING — root cause
        app.UseAuthentication();    // ← add when you wire up auth
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}