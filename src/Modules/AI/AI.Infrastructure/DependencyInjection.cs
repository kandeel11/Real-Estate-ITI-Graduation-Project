using AI.Application.Common.Options;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Data.Interceptors;
using AI.Infrastructure.Handlers;
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
        services.AddOptions<R2Options>()
                .BindConfiguration(R2Options.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        // ✅ Registered as Singleton — the S3 client is thread-safe
        services.AddSingleton<IDocumentStorageService, R2DocumentStorageService>();

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
            options.UseNpgsql(connectionString).AddInterceptors(interceptor);
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
        // 1. Register Gemini Embeddings (text-embedding-004)
        //    CHANGED: was Azure OpenAI — Azure subscription got disabled
        // ====================================================================
        services.AddHttpClient(nameof(GeminiTextEmbeddingGenerationService));
        services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GeminiTextEmbeddingGenerationService));

            // Graceful degradation: an empty API key just returns empty embeddings —
            // RAG/chatbot search stays inert until configured, everything else keeps working.
            return new GeminiTextEmbeddingGenerationService(
                http,
                aiSettings.GeminiApiKey,
                aiSettings.GeminiEmbeddingModel);
        });

        // ====================================================================
        // 2. Register Semantic Kernel Plugins
        // ====================================================================
        services.AddScoped<PropertySearchPlugin>();
        services.AddScoped<ContractRetrievalPlugin>();
        services.AddScoped<IContractDocumentService, ContractDocumentService>();
        services.AddScoped<IBlobStorage, R2BlobStorage>();
        //services.AddScoped<IOwnershipAiService, OwnershipAiService>();

        // ====================================================================
        // 3. Build Semantic Kernel (Configured for Groq Chat via OpenAI-compatible API)
        //    CHANGED: was OpenAI gpt-4o-mini — OpenAI quota ended, switched to Groq
        // ====================================================================
        services.AddScoped(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var builder = Kernel.CreateBuilder();

            // CHANGED: Groq chat model — replaced OpenAI chat completion
            // Groq exposes an OpenAI-compatible API, so we reuse the OpenAI connector
            // with a custom endpoint and the Groq API key from secrets.
            var groqClient = new OpenAI.OpenAIClient(
                new System.ClientModel.ApiKeyCredential(aiSettings.GroqApiKey),
                new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1/") }
            );
            builder.AddOpenAIChatCompletion(
                modelId: aiSettings.GroqModelId, // e.g., "llama-3.3-70b-versatile"
                openAIClient: groqClient
            );

            // OLD (OpenAI — quota ended):
            // builder.AddOpenAIChatCompletion(
            //     modelId: aiSettings.ModelVersion, // "gpt-4o-mini"
            //     apiKey: aiSettings.OpenAiApiKey
            // );

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

        services.AddHttpClient(nameof(GeminiDocumentExtractor));
        services.AddSingleton<IDocumentExtractor, GeminiDocumentExtractor>();
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
        services.AddScoped<ISmartSearchService, SmartSearchService>();
        //services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();

        // ── MediatR — auto-registers ALL handlers ─────────────────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(SmartSearchHandler).Assembly));
        services.AddScoped<IContractRetrievalService, ContractRetrievalService>();
        services.AddScoped<IPropertyDuplicateCheckService, PropertyDuplicateCheckService>();
        // ====================================================================
        // Ownership Verification Module Services
        // ====================================================================
        services.AddOptions<VerificationOptions>()
                .BindConfiguration(VerificationOptions.SectionName)
                .ValidateDataAnnotations();

        services.AddHttpClient(nameof(GeminiOwnershipAiService));
        services.AddScoped<IOwnershipAiService, GeminiOwnershipAiService>();
        services.AddScoped<IOwnershipVerifyRepository, OwnershipVerifyRepository>();
        //services.AddScoped<IDocumentStorageService, InMemoryDocumentStorageService>();

        return services;
    }
}