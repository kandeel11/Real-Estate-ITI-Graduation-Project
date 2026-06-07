using Document.Domain.Entities;
using Document.Domain.Enums;

namespace Document.Domain.Entities;

public class Inquiry
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }        // NO DB FK — ref property.properties
    public Guid TenantId { get; set; }          // NO DB FK — ref identity.users
    public Guid? CompanyId { get; set; }        // NO DB FK — ref identity.companies
    public Guid OwnerId { get; set; }           // NO DB FK — ref identity.users
    public string Message { get; set; } = null!;
    public InquiryStatus Status { get; set; } = InquiryStatus.Pending;
    public Guid? AcceptedById { get; set; }     // NO DB FK — ref identity.users
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Same-schema navigations (real FK)
    public ICollection<InquiryAssignment> Assignments { get; set; } = new List<InquiryAssignment>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}