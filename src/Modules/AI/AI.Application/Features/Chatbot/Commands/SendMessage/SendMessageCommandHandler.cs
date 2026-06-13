using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace AI.Application.Features.Chatbot.Commands.SendMessage;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendChatBotMessageCommand, SendMessageResponse>
{
    private readonly IAiDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISemanticKernelService _ai;
    private readonly ILogger<SendMessageCommandHandler> _logger;
    private readonly ISubscriptionLimitChecker _limitChecker;

    public SendMessageCommandHandler(
        IAiDbContext context,
        ICurrentUserService currentUserService,
        ISemanticKernelService ai,
        ILogger<SendMessageCommandHandler> logger,
        ISubscriptionLimitChecker limitChecker)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
        _currentUserService = currentUserService;
        _limitChecker = limitChecker;
    }

    public async Task<SendMessageResponse> Handle(
     SendChatBotMessageCommand request,
     CancellationToken cancellationToken)
    {
        // Plan feature gate first (does the plan include the chatbot at all?),
        // then the monthly request quota — both before paying for an AI call
        var quotaUserId = _currentUserService.UserId ?? Guid.Empty;
        var (featureAllowed, featureReason) = await _limitChecker.CheckAiChatbotAccessAsync(
            quotaUserId, _currentUserService.CompanyId, cancellationToken);
        if (!featureAllowed)
            throw new InvalidOperationException(featureReason);

        var (quotaAllowed, quotaReason) = await _limitChecker.CheckAiRequestLimitAsync(
            quotaUserId, _currentUserService.CompanyId, cancellationToken);
        if (!quotaAllowed)
            throw new InvalidOperationException(quotaReason);

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

        // Count this request against the plan's monthly AI quota
        await _limitChecker.IncrementAiUsageAsync(
            quotaUserId, _currentUserService.CompanyId, cancellationToken);

        return new SendMessageResponse(session.Id, aiResponse.ReplyText);
    }
}