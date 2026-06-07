using Subscription.Domain.Enums;

namespace Subscription.Application.DTOs;

public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid PlanId { get; init; }
    public Guid UserId { get; init; }
    public Guid? CompanyId { get; init; }
    public BillingCycle BillingCycle { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public bool IsAutoRenew { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
