using AI.Application.Common.Options;
using AI.Application.Features.Chat.Commands;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Data.Interceptors;
using AI.Infrastructure.Features.Chat;
using AI.Infrastructure.Persistence.Contexts;
using AI.Infrastructure.Plugins;
using AI.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

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

        // 1. Build the Kernel using configured options (Changed to AddScoped)
        services.AddScoped(sp =>
        {
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

#pragma warning disable SKEXP0010
            builder.AddOpenAIEmbeddingGenerator(
                modelId: "text-embedding-3-small",
                apiKey: aiSettings.OpenAiApiKey
            );
#pragma warning restore SKEXP0010

            // Resolve Plugins from the main DI container so they can access Scoped Repositories
            var propertyPlugin = sp.GetRequiredService<PropertySearchPlugin>();
            var contractPlugin = sp.GetRequiredService<ContractRetrievalPlugin>();

            builder.Plugins.AddFromObject(propertyPlugin, "PropertySearch");
            builder.Plugins.AddFromObject(contractPlugin, "ContractRetrieval");

            return builder.Build();
        });

        // ====================================================================
        // Register Plugins as Scoped Services in the DI Container
        // ====================================================================
        services.AddScoped<PropertySearchPlugin>();
        services.AddScoped<ContractRetrievalPlugin>();

        services.AddOptions<AzureDocumentIntelligenceOptions>()
                .BindConfiguration(AzureDocumentIntelligenceOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // Swap this line to switch extractor without touching anything else
        services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();
        // 2. Bridge: Extract SK services to make them available in the main DI Container
        services.AddTransient<IChatCompletionService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        services.AddTransient<ITextEmbeddingGenerationService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        });

        // 3. Configure MediatR
        services.AddMediatR(cfg =>
        {
            // Application layer — Commands, Queries, Validators
            cfg.RegisterServicesFromAssembly(
                typeof(SendMessageCommand).Assembly);

            // Infrastructure layer — Handlers that need SK/OpenAI packages
            cfg.RegisterServicesFromAssembly(
                typeof(SendMessageCommandHandler).Assembly);
        });

        // 4. Session management
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        // Production: services.AddSingleton<ISessionStore, RedisSessionStore>();

        // 5. Qdrant and RAG Services Configuration
        var qdrantEndpoint = configuration["Qdrant:Endpoint"];
        var qdrantApiKey = configuration["Qdrant:ApiKey"];

        if (string.IsNullOrEmpty(qdrantEndpoint) || string.IsNullOrEmpty(qdrantApiKey))
        {
            throw new ArgumentNullException("Qdrant configuration is missing in appsettings.json");
        }

        // Register the underlying Qdrant Client
        services.AddSingleton<QdrantClient>(sp =>
            new QdrantClient(host: qdrantEndpoint, apiKey: qdrantApiKey, https: true));

        // Register the SK Qdrant Vector Store
#pragma warning disable SKEXP0020
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<QdrantClient>();
            return new QdrantVectorStore(client, ownsClient: false);
        });
#pragma warning restore SKEXP0020

        // Register the contract retrieval service
        services.AddScoped<IContractRetrievalService, ContractRetrievalService>();

        return services;
    }
}