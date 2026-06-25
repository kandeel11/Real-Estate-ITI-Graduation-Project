using Document.Domain.Entities;
using MediatR;

namespace Document.Domain.Events;

public abstract class DomainEvent : INotification
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

public record LeaseUploadedEvent(
    Guid LeaseId,
    string ContractFileUrl,
    Guid OwnerId) : INotification;

public record LeaseExtractionCompletedEvent(
    Guid LeaseId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    decimal? Confidence) : INotification;

public sealed record LeaseActivatedEvent(
    Guid LeaseId,
    Guid PropertyId,
    Guid OwnerId,
    decimal InstallmentAmount,
    int PaymentDayOfMonth,
    DateOnly StartDate,
    DateOnly EndDate,
    string BlobPath,     // Mohamed reads this to embed into Qdrant
    bool IsFullManagement = false,
    bool IsMaintenanceEnabled = false,
    bool IsPaymentEnabled = false
) : INotification;