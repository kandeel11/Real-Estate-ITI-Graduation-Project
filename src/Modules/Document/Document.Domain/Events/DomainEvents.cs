namespace Document.Domain.Events;

public abstract class DomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class InquiryAcceptedEvent : DomainEvent
{
    public Guid InquiryId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid TenantId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid AcceptedById { get; init; }
}

public class LeadClosedWonEvent : DomainEvent
{
    public Guid LeadId { get; init; }
    public Guid InquiryId { get; init; }
    public Guid PropertyId { get; init; }
    public Guid TenantId { get; init; }
    public Guid OwnerId { get; init; }
    public decimal? DealValue { get; init; }
}

public class RatingApprovedEvent : DomainEvent
{
    public Guid RatingId { get; init; }
    public Guid ReviewerId { get; init; }
    public Guid? BrokerId { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? MaintenanceCompanyId { get; init; }
    public int RatingValue { get; init; }
}

public record LeaseUploadedEvent(Guid LeaseId, string ContractFileUrl, Guid OwnerId);
public record LeaseActivatedEvent(Guid LeaseId, Guid PropertyId, Guid TenantId, Guid OwnerId);