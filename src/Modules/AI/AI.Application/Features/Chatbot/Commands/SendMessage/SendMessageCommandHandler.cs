using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;

namespace AI.Application.Features.Chatbot.Commands.SendMessage
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResponse>
    {
        private readonly IAiDbContext _context;
        private readonly ISemanticKernelService _semanticKernelService;
        private readonly ILogger<SendMessageCommandHandler> _logger;

        public SendMessageCommandHandler(IAiDbContext context, ISemanticKernelService semanticKernelService, ILogger<SendMessageCommandHandler> logger)
        {
            _context = context;
            _semanticKernelService = semanticKernelService;
            _logger = logger;
        }
        public async Task<SendMessageResponse> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            ChatbotSession session;
            var chatHistory = new ChatHistory();

            // ==========================================
            // 1. Retrieve Existing Session or Create New
            // ==========================================
            if (request.SessionId.HasValue)
            {
                // Fetch the session with its messages ordered by creation time
                session = await _context.ChatbotSessions
                    .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value, cancellationToken);

                if (session == null)
                {
                    throw new Exception($"Session with ID {request.SessionId} not found.");
                }

                // Rebuild the ChatHistory for the AI to maintain context
                foreach (var msg in session.Messages)
                {
                    if (msg.Role == "user") chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant") chatHistory.AddAssistantMessage(msg.Content);
                }
            }
            else
            {
                // Create a completely new session
                session = new ChatbotSession
                {
                    //Id = Guid.NewGuid(), // Explicitly generate ID to avoid EF insertion issues
                    UserId = request.UserId,
                    SessionToken = Guid.NewGuid().ToString(),
                    Title = request.Message.Length > 20 ? request.Message.Substring(0, 20) + "..." : request.Message
                };

                _context.ChatbotSessions.Add(session);
            }

            // ==========================================
            // 2. Add the New User Message
            // ==========================================
            var userMessage = new ChatbotMessage
            {
                //Id = Guid.NewGuid(), // Explicitly set ID
                SessionId = session.Id, // Use Foreign Key instead of Navigation Property (session.Messages)
                Role = "user",
                Content = request.Message,
                TokensUsed = 0
            };

            // Add directly to the DbSet to prevent parent modification tracking issues
            _context.ChatbotMessages.Add(userMessage);

            // Append to the AI memory
            chatHistory.AddUserMessage(request.Message);

            // ==========================================
            // 3. Communicate with AI Service
            // ==========================================
            _logger.LogInformation("Sending message to AI. SessionId: {SessionId}", session.Id);
            var aiResponse = await _semanticKernelService.SendChatMessageAsync(chatHistory, cancellationToken);

            // ==========================================
            // 4. Add the Assistant Message
            // ==========================================
            var assistantMessage = new ChatbotMessage
            {
                //Id = Guid.NewGuid(),
                SessionId = session.Id, // Explicit Foreign Key
                Role = "assistant",
                Content = aiResponse.ReplyText,
                TokensUsed = aiResponse.TokensUsed
            };

            _context.ChatbotMessages.Add(assistantMessage);

            // ==========================================
            // 5. Commit Changes to Database
            // ==========================================
            await _context.SaveChangesAsync(cancellationToken);

            // Return the response containing the AI reply and the active Session ID
            return new SendMessageResponse(session.Id, aiResponse.ReplyText);
        }
    }
}
