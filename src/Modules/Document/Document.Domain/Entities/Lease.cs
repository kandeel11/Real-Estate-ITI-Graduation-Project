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
    public decimal? RentAmount { get; set; }
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
}