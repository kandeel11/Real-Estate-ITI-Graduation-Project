using AI.Application.Features.AdminAnalytics.Plugins;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Features.AdminAnalytics;

public sealed class AdminAnalyticsQueryHandler
    : IRequestHandler<AdminAnalyticsQuery, AdminAnalyticsResponse>
{
    private readonly Kernel _kernel;
    private readonly AdminAnalyticsPlugin _plugin;
    private readonly ILogger<AdminAnalyticsQueryHandler> _logger;

    // System prompt scoped to admin analytics — separate identity from the
    // user-facing chatbot which is restricted to property/lease search.
    private const string AdminSystemPrompt = """
        You are an AI-powered Admin Analytics Assistant for a Real Estate and Property Management platform.

        You have access to live system data through the tools provided to you:
        - Property analytics: total counts, status breakdown, new listings by date, top cities, type breakdown.
        - Payment analytics: rent revenue summary, overdue payments, revenue in a date range.
        - Subscription analytics: active/trial/cancelled counts, plan distribution, new and churned subscriptions.
        - User analytics: total registered users, companies, brokers, and new registrations by date range.

        STRICT RULES:
        - ALWAYS call the relevant tool before answering any data question. Never invent, estimate, or guess numbers.
        - If a question needs a date range and the admin has not specified one, ask for clarification.
        - Respond only in English.
        - Be concise and data-driven. Lead with the key number(s), then add a brief sentence of context if useful.
        - If the admin asks something outside your data domain (writing code, general knowledge, etc.), politely decline.
        """;

    public AdminAnalyticsQueryHandler(
        Kernel kernel,
        AdminAnalyticsPlugin plugin,
        ILogger<AdminAnalyticsQueryHandler> logger)
    {
        _kernel = kernel;
        _plugin = plugin;
        _logger = logger;
    }

    public async Task<AdminAnalyticsResponse> Handle(
        AdminAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        // Clone the scoped kernel so the shared instance (which carries
        // PropertySearchPlugin and ContractRetrievalPlugin) is not mutated.
        var adminKernel = _kernel.Clone();

        // Strip all inherited plugins — admin analytics has its own tool set.
        foreach (var p in adminKernel.Plugins.ToList())
            adminKernel.Plugins.Remove(p);

        adminKernel.Plugins.AddFromObject(_plugin, "AdminAnalytics");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(AdminSystemPrompt);
        chatHistory.AddUserMessage(request.Prompt);

        // FunctionChoiceBehavior is provider-agnostic (SemanticKernel.Core).
        // This avoids importing Microsoft.SemanticKernel.Connectors.OpenAI here.
        var executionSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        _logger.LogInformation(
            "Admin analytics query started. Prompt: {Prompt}", request.Prompt);

        var chatService = adminKernel.GetRequiredService<IChatCompletionService>();

        ChatMessageContent response;
        try
        {
            response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel: adminKernel,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin analytics AI call failed for prompt: {Prompt}", request.Prompt);
            throw new InvalidOperationException(
                "The analytics assistant is temporarily unavailable. Please try again later.");
        }

        var answer = response.Content
            ?? "I was unable to generate a response. Please try again.";

        _logger.LogInformation("Admin analytics query completed successfully.");

        return new AdminAnalyticsResponse(answer);
    }
}
