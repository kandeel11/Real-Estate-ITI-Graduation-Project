using Document.Domain.Enums;

namespace Document.Domain.Entities;

public class Lease
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? BrokerId { get; set; }
    public Guid? InquiryId { get; set; }
    public Guid? LeadId { get; set; }
    public decimal? InstallmentAmount { get; set; }
    public PaymentFrequency? PaymentFrequency { get; set; }
    public decimal? TotalRentAmount { get; set; }
    public int? DurationInMonths { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? PaymentDayOfMonth { get; set; }
    public string ContractFileUrl { get; set; } = null!;
    public DateTime? TenantSignedAt { get; set; }
    public DateTime? OwnerSignedAt { get; set; }
    public string ExtractionStatus { get; set; } = "pending";
    public decimal? ExtractionConfidence { get; set; }
    public Guid? ConfirmedById { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsActive { get; set; } = false;
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Inquiry? Inquiry { get; set; }
    public Lead? Lead { get; set; }
    public DateTime? OwnerConfirmedAt { get; set; }
    public DateTime? TenantConfirmedAt { get; set; }
    public string? RejectionReason { get; set; }

    // ── Renewal ──────────────────────────────────────────────────────────────
    /// <summary>When set, this lease was created as a renewal of the referenced predecessor.</summary>
    public Guid? RenewedFromLeaseId { get; set; }
    /// <summary>Set when the tenant asks to renew; cleared once the landlord uploads the renewal lease.</summary>
    public DateTime? RenewalRequestedAt { get; set; }

    // ── Early termination (mutual consent) ───────────────────────────────────
    /// <summary>The party (tenant or owner/broker) who requested early termination, if any.</summary>
    public Guid? TerminationRequestedById { get; set; }
    public DateTime? TerminationRequestedAt { get; set; }
    /// <summary>Proposed date the lease should end if the other party approves.</summary>
    public DateOnly? TerminationEffectiveDate { get; set; }

    // ── Full-management workflow (Company Broker only) ────────────────────────
    public bool IsFullManagement { get; set; }
    public string? ManagementOptions { get; set; }
    public string? ManagerApprovalStatus { get; set; }  // "Pending" | "Approved" | "Rejected"
    public Guid? ManagerId { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public DateTime? ManagerRejectedAt { get; set; }
    public string? ManagerNotes { get; set; }
}