using AI.Application.Common.Options;
using AI.Application.Features.Chat.Commands;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Infrastructure.Data.Interceptors;
using AI.Infrastructure.Features.Chat;
using AI.Infrastructure.Persistence.Contexts;
using AI.Infrastructure.Persistence.Repositories;
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
using System.Net.Http;

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
            .ValidateDataAnnotations();

        // ====================================================================
        // 1. تسجيل Azure Embeddings كخدمة مستقلة (عشان نكسر الـ Deadlock تماماً)
        // ====================================================================
#pragma warning disable SKEXP0011
        services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;

            return new Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAITextEmbeddingGenerationService(
                aiSettings.AzureEmbeddingDeploymentName,
                aiSettings.AzureEmbeddingEndpoint,
                aiSettings.AzureEmbeddingApiKey
            );
        });
#pragma warning restore SKEXP0011

        // ====================================================================
        // 2. تسجيل الـ Plugins (دلوقتي هيقدروا يقرأوا الـ Embeddings بأمان)
        // ====================================================================
        services.AddScoped<PropertySearchPlugin>();
        services.AddScoped<ContractRetrievalPlugin>();
        services.AddScoped<IContractDocumentService, ContractDocumentService>();

        // ====================================================================
        // 3. بناء الـ Kernel (دمج Groq مع الـ Plugins والـ Embeddings)
        // ====================================================================
        services.AddScoped(sp =>
        {
            var aiSettings = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            var builder = Kernel.CreateBuilder();

            // إضافة Groq للـ Chat
#pragma warning disable SKEXP0070
            builder.AddOpenAIChatCompletion(
                modelId: aiSettings.GroqModelId,
                apiKey: aiSettings.GroqApiKey,
                httpClient: new HttpClient { BaseAddress = new Uri("https://api.groq.com/openai/v1/") }
            );
#pragma warning restore SKEXP0070

            // إضافة الـ Embeddings المستقلة جوه الـ Kernel
            builder.Services.AddSingleton(sp.GetRequiredService<ITextEmbeddingGenerationService>());

            // إضافة الـ Plugins
            builder.Plugins.AddFromObject(sp.GetRequiredService<PropertySearchPlugin>());
            builder.Plugins.AddFromObject(sp.GetRequiredService<ContractRetrievalPlugin>());

            return builder.Build();
        });
        // AI.Infrastructure/DependencyInjection.cs — add registration

        services.AddScoped<IIndexManagementService, IndexManagementService>();
        // ====================================================================
        // 
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
        // Qdrant Vector Store
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
        // Ownership Verification Services
        // ====================================================================
        services.AddOptions<VerificationOptions>()
                .BindConfiguration(VerificationOptions.SectionName)
                .ValidateDataAnnotations();

        services.AddScoped<IOwnershipAiService, AzureDocumentIntelligenceOwnershipAiService>();
        services.AddScoped<IOwnershipVerifyRepository, OwnershipVerifyRepository>();
        services.AddScoped<IDocumentStorageService, InMemoryDocumentStorageService>();

        return services;
    }
}