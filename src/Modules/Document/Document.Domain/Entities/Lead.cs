using Document.Domain.Enums;

namespace Document.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; }
    public Guid InquiryId { get; set; }         // Real FK → document.inquiries
    public Guid OwnerId { get; set; }           // NO DB FK — ref identity.users
    public Guid PropertyId { get; set; }        // NO DB FK — ref property.properties
    public Guid TenantId { get; set; }          // NO DB FK — ref identity.users
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public DateTime? ClosedAt { get; set; }
    public decimal? DealValue { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Inquiry Inquiry { get; set; } = null!;

    // ── No-management deal flow ──────────────────────────────────────────────
    /// <summary>Set when a company broker chooses "no management"; cleared after manager confirms.</summary>
    public DateTime? NoManagementPendingAt { get; set; }
    /// <summary>Company manager who must confirm the no-management decision.</summary>
    public Guid? NoManagementManagerId { get; set; }
}