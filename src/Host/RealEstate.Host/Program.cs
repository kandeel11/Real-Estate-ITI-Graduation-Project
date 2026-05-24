using AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription;
using AI.Infrastructure;

namespace RealEstate.Host;

public class Program
{
    public static void Main(string[] args)
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

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate Management API v1");
            });
        }
        // Configure the HTTP request pipeline.
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }
}

