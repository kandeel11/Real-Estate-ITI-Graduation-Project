using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Features.Chatbot.Commands.SendMessage;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private readonly IAiDbContext _context;
    private readonly ISemanticKernelService _ai;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IAiDbContext context,
        ISemanticKernelService ai,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
    }

    public async Task<SendMessageResponse> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        AiChatbotSession session;
        var chatHistory = new ChatHistory();

        // ── 1. Load existing session OR create a new one ────────────────────
        if (request.SessionId.HasValue)
        {
            var existing = await _context.AiChatbotSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(
                    s => s.Id == request.SessionId.Value,
                    cancellationToken);

            if (existing is null)
                throw new InvalidOperationException(
                    $"Session with ID {request.SessionId} not found.");

            if (!existing.IsActive)
                throw new InvalidOperationException(
                    $"Session {existing.Id} is already ended.");

            session = existing;

            // Rebuild ChatHistory in chronological order
            foreach (var msg in session.Messages.OrderBy(m => m.CreatedAt))
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    chatHistory.AddAssistantMessage(msg.Content);
            }
        }
        else
        {
            session = AiChatbotSession.Create(
                userId: request.UserId,
                sessionToken: Guid.NewGuid().ToString());

            _context.AiChatbotSessions.Add(session);
        }

        // ── 2. Append user message via aggregate behavior ───────────────────
        var userMessage = AiChatbotMessage.CreateUserMessage(
            sessionId: session.Id,
            content: request.Message);

        session.AddMessage(userMessage);
        chatHistory.AddUserMessage(request.Message);

        // ── 3. Call the AI ──────────────────────────────────────────────────
        _logger.LogInformation(
            "Sending message to AI. SessionId: {SessionId}", session.Id);

        var aiResponse = await _ai.SendChatMessageAsync(chatHistory, cancellationToken);

        // ── 4. Append assistant message ─────────────────────────────────────
        var assistantMessage = AiChatbotMessage.CreateAssistantMessage(
            sessionId: session.Id,
            content: aiResponse.ReplyText,
            tokensUsed: aiResponse.TokensUsed);

        session.AddMessage(assistantMessage);

        // ── 5. Persist ──────────────────────────────────────────────────────
        await _context.SaveChangesAsync(cancellationToken);

        return new SendMessageResponse(session.Id, aiResponse.ReplyText);
    }
}