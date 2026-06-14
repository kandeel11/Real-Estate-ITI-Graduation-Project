using Document.Domain.Enums;

namespace Document.Domain.Entities;

public class Rating
{
    public Guid Id { get; set; }
    public Guid ReviewerId { get; set; }            // NO DB FK — ref identity.users
    public Guid? LeaseId { get; set; }              // NO DB FK — ref payment.leases
    public Guid? PropertyId { get; set; }           // NO DB FK — ref property.properties
    public Guid? CompanyId { get; set; }            // NO DB FK — ref identity.companies
    public Guid? BrokerId { get; set; }             // NO DB FK — ref identity.brokers
    public Guid? MaintenanceCompanyId { get; set; } // NO DB FK — ref identity.maintenance_companies
    public int RatingValue { get; set; }            // 1–5
    public string? ReviewText { get; set; }
    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.Pending;
    public Guid? ModeratedById { get; set; }        // NO DB FK — ref identity.users (admin)
    public DateTime? ModeratedAt { get; set; }
    public DateTime? DeletedAt { get; set; }        // Soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}