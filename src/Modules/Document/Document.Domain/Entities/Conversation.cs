namespace Document.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid? InquiryId { get; set; }        // Real FK → document.inquiries (nullable)
    public Guid PropertyId { get; set; }        // NO DB FK — ref property.properties
    public Guid TenantId { get; set; }          // NO DB FK — ref identity.users
    public Guid BrokerOrOwnerId { get; set; }   // NO DB FK — ref identity.users
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Inquiry? Inquiry { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}