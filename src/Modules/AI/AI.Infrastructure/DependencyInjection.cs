using AI.Application.Interfaces.Common;
using AI.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AiDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IAiDbContext>(provider => provider.GetRequiredService<AiDbContext>());
        return services;
    }
}
