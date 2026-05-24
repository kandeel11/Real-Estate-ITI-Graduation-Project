using Microsoft.OpenApi.Models;
using Property.Infrastructure;

namespace RealEstate.Host;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Real Estate API",
                Version = "v1",
                Description = "Real Estate Management Platform — Property Module"
            });

            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
        });

        builder.Services.AddPropertyModule(builder.Configuration);

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate API v1");
            options.RoutePrefix = string.Empty;
        });

        app.UseHttpsRedirection();
        app.MapControllers();

        app.Run();
    }
}
