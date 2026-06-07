using Document.Domain.Enums;

namespace Document.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }            // NO DB FK — ref identity.users
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}