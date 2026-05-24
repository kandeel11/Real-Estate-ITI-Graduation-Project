using AI.Application.Common.Options;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Data.Interceptors;
using AI.Infrastructure.Persistence.Contexts;
using AI.Infrastructure.Plugins;
using AI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddScoped<AuditEntitiesInterceptor>();
        services.AddDbContext<AiDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditEntitiesInterceptor>();
            options.UseSqlServer(connectionString).AddInterceptors(interceptor);
        });
        services.AddTransient<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<IAiDbContext>(provider => provider.GetRequiredService<AiDbContext>());

        services.AddOptions<AiOptions>()
           .BindConfiguration(AiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart(); 

        // 2. بناء الـ Kernel باستخدام الـ Options
        services.AddSingleton(sp =>
        {
            // سحب الـ Options من الـ DI Container
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;

            if (string.IsNullOrEmpty(aiSettings.OpenAiApiKey))
            {
                throw new InvalidOperationException("OpenAI API Key is missing from configuration.");
            }

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: aiSettings.ModelVersion,
                apiKey: aiSettings.OpenAiApiKey
            );
            builder.Plugins.AddFromType<PropertySearchPlugin>("PropertySearch");

            return builder.Build();
        });

        return services;
    }
}
