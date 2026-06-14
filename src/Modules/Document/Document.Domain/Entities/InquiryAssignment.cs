namespace Document.Domain.Entities;

public class InquiryAssignment
{
    public Guid Id { get; set; }
    public Guid InquiryId { get; set; }         // Real FK → document.inquiries
    public Guid AssignedToId { get; set; }      // NO DB FK — ref identity.brokers
    public Guid AssignedById { get; set; }      // NO DB FK — ref identity.users
    public bool IsAccepted { get; set; } = false;
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Inquiry Inquiry { get; set; } = null!;
}