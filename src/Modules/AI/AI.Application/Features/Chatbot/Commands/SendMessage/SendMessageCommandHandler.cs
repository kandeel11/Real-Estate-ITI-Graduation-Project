using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AI.Application.Features.Chatbot.Commands.SendMessage;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendChatBotMessageCommand, SendMessageResponse>
{
    private readonly IAiDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISemanticKernelService _ai;
    private readonly ILogger<SendMessageCommandHandler> _logger;

    public SendMessageCommandHandler(
        IAiDbContext context,
        ICurrentUserService currentUserService,
        ISemanticKernelService ai,
        ILogger<SendMessageCommandHandler> logger)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<SendMessageResponse> Handle(
     SendChatBotMessageCommand request,
     CancellationToken cancellationToken)
    {
        //request.UserId = _currentUserService.UserId;
        // استخدمنا الـ Entity الخاص بالشات العادي
        ChatbotSession session;
        var chatHistory = new ChatHistory();

        // ── 1. Load existing session OR create a new one ────────────────────
        if (request.SessionId.HasValue)
        {
            var existing = await _context.ChatbotSessions // استخدام الـ DbSet الصحيح
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(
                    s => s.Id == request.SessionId.Value,
                    cancellationToken);

            if (existing is null)
                throw new InvalidOperationException($"Session with ID {request.SessionId} not found.");

            // بافتراض إن الـ Property دي موجودة عندك
            if (!existing.IsActive)
                throw new InvalidOperationException($"Session {existing.Id} is already ended.");

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
            // بافتراض إنك بتستخدم Factory Method أو Constructor زي ما كنت عامل
            session = ChatbotSession.Create(
                userId: _currentUserService.UserId??Guid.Empty,
                sessionToken: Guid.NewGuid().ToString());

            _context.ChatbotSessions.Add(session); // إضافة الـ Session الجديدة للجدول الصح
        }

        // ── 2. Create and Append User Message DIRECTLY to DbSet ───────────────
        var userMessage = ChatbotMessage.CreateUserMessage(
            sessionId: session.Id,
            content: request.Message);

        // ✅ التعديل هنا: الإضافة للجدول الخاص برسائل الشات العادي مباشرة
        _context.ChatbotMessages.Add(userMessage);
        chatHistory.AddUserMessage(request.Message);

        // ── 3. Call the AI ──────────────────────────────────────────────────
        _logger.LogInformation("Sending message to AI. SessionId: {SessionId}", session.Id);

        var aiResponse = await _ai.SendChatMessageAsync(chatHistory, cancellationToken);

        // ── 4. Create and Append Assistant Message DIRECTLY to DbSet ──────────
        var assistantMessage = ChatbotMessage.CreateAssistantMessage(
            sessionId: session.Id,
            content: aiResponse.ReplyText,
            tokensUsed: aiResponse.TokensUsed);

        // ✅ التعديل هنا: الإضافة للجدول مباشرة
        _context.ChatbotMessages.Add(assistantMessage);

        // ── 5. Persist ──────────────────────────────────────────────────────
        await _context.SaveChangesAsync(cancellationToken);

        return new SendMessageResponse(session.Id, aiResponse.ReplyText);
    }
}