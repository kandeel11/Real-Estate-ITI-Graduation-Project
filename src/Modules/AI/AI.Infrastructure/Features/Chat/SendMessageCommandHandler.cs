// ✅ CREATE in AI.Infrastructure
// AI.Infrastructure/Features/Chat/SendMessageCommandHandler.cs

using AI.Application.DTOs;
using AI.Application.Features.Chat.Commands;
using AI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;   // ✅ lives in Infrastructure

namespace AI.Infrastructure.Features.Chat;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;
    private readonly ISessionStore _sessions;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    private const string SystemPrompt = """
        You are a legal AI assistant specialized in analyzing contracts.
        You have access to a contract database through semantic search tools.

        Guidelines:
        - Always search the contract database before answering questions about clauses or terms.
        - Cite the page number and contract ID when referencing specific clauses.
        - If you cannot find relevant information, clearly state that.
        - Be precise and professional in your legal language.
        - Never fabricate contract terms — only use what the search returns.
        """;

    public SendMessageCommandHandler(
        Kernel kernel,
        IChatCompletionService chat,
        ISessionStore sessions,
        ILogger<SendMessageCommandHandler> logger)
    {
        _kernel = kernel;
        _chat = chat;
        _sessions = sessions;
        _logger = logger;
    }

    public async Task<SendMessageResult> Handle(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling SendMessageCommand | Session={SessionId} | ContractId={ContractId}",
            command.SessionId, command.ContractId);

        // ── 1. Restore or create conversation history ────────────────────────
        var history = await _sessions.GetOrCreateAsync(
            command.SessionId, SystemPrompt, cancellationToken);

        // ── 2. Inject contract scope hint if provided ────────────────────────
        var userMessage = command.ContractId.HasValue
            ? $"[Contract ID: {command.ContractId}]\n{command.Message}"
            : command.Message;

        history.AddUserMessage(userMessage);

        // ── 3. AutoInvokeKernelFunctions ─────────────────────────────────────
#pragma warning disable SKEXP0001
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.2,
            MaxTokens = 2000,
            TopP = 0.95
        };
#pragma warning restore SKEXP0001

        // ── 4. Execute — plugins auto-invoked by Kernel ──────────────────────
        var response = await _chat.GetChatMessageContentAsync(
            chatHistory: history,
            executionSettings: settings,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        // ── 5. Persist updated history ───────────────────────────────────────
        history.AddAssistantMessage(response.Content ?? string.Empty);
        await _sessions.SaveAsync(command.SessionId, history, cancellationToken);

        // ── 6. Extract token usage ───────────────────────────────────────────
        var usage = response.Metadata
            ?.GetValueOrDefault("Usage") as OpenAI.Chat.ChatTokenUsage;

        _logger.LogInformation(
            "Response ready | Session={SessionId} | Tokens={In}+{Out}",
            command.SessionId,
            usage?.InputTokenCount ?? 0,
            usage?.OutputTokenCount ?? 0);

        return new SendMessageResult(
            SessionId: command.SessionId,
            Answer: response.Content ?? string.Empty,
            PromptTokens: usage?.InputTokenCount ?? 0,
            CompletionTokens: usage?.OutputTokenCount ?? 0);
    }
}