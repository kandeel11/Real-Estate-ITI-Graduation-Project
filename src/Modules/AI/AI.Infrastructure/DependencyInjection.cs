using AI.Application.Common.Options;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Data.Interceptors;
using AI.Infrastructure.Persistence.Contexts;
using AI.Infrastructure.Persistence.Repositories;
using AI.Infrastructure.Plugins;
using AI.Infrastructure.Services;
using AI.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

namespace AI.Infrastructure;

public static class DependencyInjection
{

    private static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AzureBlobOptions>()
                .BindConfiguration(AzureBlobOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // ✅ Registered as Singleton — BlobContainerClient is thread-safe
        services.AddSingleton<IDocumentStorageService, AzureBlobDocumentStorageService>();

        return services;
    }

    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // ====================================================================
        // Database & Interceptors Registration
        // ====================================================================
        services.AddScoped<AuditEntitiesInterceptor>();
        services.AddDbContext<AiDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditEntitiesInterceptor>();
            options.UseSqlServer(connectionString).AddInterceptors(interceptor);
        });
        services
    .AddStorageServices(configuration);

        services.AddTransient<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<IAiDbContext>(provider => provider.GetRequiredService<AiDbContext>());
        
        // Bind AI Configuration Options
        services.AddOptions<AiOptions>()
           .BindConfiguration(AiOptions.SectionName)
           .ValidateDataAnnotations()
           ;

        // ====================================================================
        // 1. Register OpenAI Embeddings as a Standalone Service
        // ====================================================================
#pragma warning disable SKEXP0011
        services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;

            return new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAITextEmbeddingGenerationService(
                aiSettings.EmbeddingModelVersion, // e.g., "text-embedding-3-small"
                aiSettings.OpenAiApiKey
            );
        });
#pragma warning restore SKEXP0011

        // ====================================================================
        // 2. Register Semantic Kernel Plugins
        // ====================================================================
        services.AddScoped<PropertySearchPlugin>();
        services.AddScoped<ContractRetrievalPlugin>();
        services.AddScoped<IContractDocumentService, ContractDocumentService>();

        // ====================================================================
        // 3. Build Semantic Kernel (Configured for OpenAI Chat)
        // ====================================================================
        services.AddScoped(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var builder = Kernel.CreateBuilder();

            // Register OpenAI Chat Completion Service
            builder.AddOpenAIChatCompletion(
                modelId: aiSettings.ModelVersion, // e.g., "gpt-4o-mini"
                apiKey: aiSettings.OpenAiApiKey
            );

            // Inject the standalone Embeddings service into the Kernel
            builder.Services.AddSingleton(sp.GetRequiredService<ITextEmbeddingGenerationService>());

            // Register Plugins to the Kernel
            builder.Plugins.AddFromObject(sp.GetRequiredService<PropertySearchPlugin>());
            builder.Plugins.AddFromObject(sp.GetRequiredService<ContractRetrievalPlugin>());

            return builder.Build();
        });

        services.AddScoped<IIndexManagementService, IndexManagementService>();

        // ====================================================================
        // Shared AI Services & Session Stores
        // ====================================================================
        services.AddTransient<IChatCompletionService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        services.AddOptions<AzureDocumentIntelligenceOptions>()
                .BindConfiguration(AzureDocumentIntelligenceOptions.SectionName)
                .ValidateDataAnnotations();

        services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddScoped<ILeaseExtractionService, LeaseExtractionService>();

        // ====================================================================
        // Qdrant Vector Store Integration
        // ====================================================================
        var qdrantEndpoint = configuration["Qdrant:Endpoint"];
        var qdrantApiKey = configuration["Qdrant:ApiKey"];

        services.AddSingleton<QdrantClient>(sp =>
            new QdrantClient(host: qdrantEndpoint, apiKey: qdrantApiKey, https: true));

#pragma warning disable SKEXP0020
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<QdrantClient>();
            return new QdrantVectorStore(client, ownsClient: false);
        });
#pragma warning restore SKEXP0020

        services.AddScoped<IContractRetrievalService, ContractRetrievalService>();

        // ====================================================================
        // Ownership Verification Module Services
        // ====================================================================
        services.AddOptions<VerificationOptions>()
                .BindConfiguration(VerificationOptions.SectionName)
                .ValidateDataAnnotations();

        services.AddScoped<IOwnershipAiService, AzureDocumentIntelligenceOwnershipAiService>();
        services.AddScoped<IOwnershipVerifyRepository, OwnershipVerifyRepository>();
        //services.AddScoped<IDocumentStorageService, InMemoryDocumentStorageService>();

        return services;
    }
}