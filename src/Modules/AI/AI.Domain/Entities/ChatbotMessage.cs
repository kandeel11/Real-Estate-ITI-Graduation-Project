using Domain.Entities;
using System;

namespace AI.Domain.Entities
{
    public class ChatbotMessage : BaseEntity
    {
        public Guid SessionId { get; private set; }

        public string Role { get; private set; } = string.Empty;

        public string Content { get; private set; } = string.Empty;

        public int TokensUsed { get; private set; }

        public virtual ChatbotSession Session { get; private set; } = null!;

        // Parameterless constructor required by EF Core
        protected ChatbotMessage() { }

        // Factory Method لرسالة المستخدم
        public static ChatbotMessage CreateUserMessage(Guid sessionId, string content)
        {
            return new ChatbotMessage
            {
                SessionId = sessionId,
                Role = "user",
                Content = content,
                TokensUsed = 0
            };
        }

        // Factory Method لرسالة الذكاء الاصطناعي
        public static ChatbotMessage CreateAssistantMessage(Guid sessionId, string content, int tokensUsed)
        {
            return new ChatbotMessage
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = content,
                TokensUsed = tokensUsed
            };
        }
    }
}