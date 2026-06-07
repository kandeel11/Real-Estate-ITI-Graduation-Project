// AI.Domain/Entities/AiChatbotMessage.cs

using Domain.Entities;

namespace AI.Domain.Entities;

public class AiChatbotMessage : BaseEntity
{
    public Guid SessionId { get; private set; }
    public string Role { get; private set; } = string.Empty;  // "user" | "assistant" | "system"
    public string Content { get; private set; } = string.Empty;
    public int TokensUsed { get; private set; }

    public virtual AiChatbotSession Session { get; private set; } = default!;

    protected AiChatbotMessage() { }

    public static AiChatbotMessage CreateUserMessage(Guid sessionId, string content) =>
        new() { SessionId = sessionId, Role = "user", Content = content, TokensUsed = 0 };

    public static AiChatbotMessage CreateAssistantMessage(
        Guid sessionId, string content, int tokensUsed) =>
        new() { SessionId = sessionId, Role = "assistant", Content = content, TokensUsed = tokensUsed };
}