// AI.Domain/Entities/AiChatbotSession.cs

using Domain.Entities;
using MediatR;

namespace AI.Domain.Entities;

public class AiChatbotSession : BaseEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid UserId { get; private set; }
    public string SessionToken { get; private set; } = string.Empty;

    // ── Status ────────────────────────────────────────────────────────────────
    public bool IsActive { get; private set; } = true;
    public DateTime? EndedAt { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual ICollection<AiChatbotMessage> Messages { get; private set; }
        = new List<AiChatbotMessage>();

    // ── EF Core constructor ───────────────────────────────────────────────────
    protected AiChatbotSession() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    public static AiChatbotSession Create(
        Guid userId,
        string sessionToken) => new()
        {
            UserId = userId,
            SessionToken = sessionToken,
            IsActive = true
        };

    // ── Behaviors ─────────────────────────────────────────────────────────────
    public void End()
    {
        if (!IsActive) return;

        IsActive = false;
        EndedAt = DateTime.UtcNow;
    }

    public void AddMessage(AiChatbotMessage message)
        => Messages.Add(message);
}