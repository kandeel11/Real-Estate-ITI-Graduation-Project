namespace Document.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }    // Real FK → document.conversations
    public Guid SenderId { get; set; }          // NO DB FK — ref identity.users
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime? DeletedAt { get; set; }    // Soft delete
    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}