using Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Domain.Events;

namespace Subscription.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid PlanId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }

    public BillingCycle BillingCycle { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime? TrialEndDate { get; private set; }

    public bool IsAutoRenew { get; private set; } = true;
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public Guid? UpgradeFromPlanId { get; private set; }
    public Guid? DowngradeToPlanId { get; private set; }

    public SubscriptionPlan Plan { get; private set; } = null!;

    private readonly List<SubscriptionPayment> _payments = new();
    public ICollection<SubscriptionPayment> Payments => _payments.AsReadOnly();

    private readonly List<SubscriptionUsage> _usageRecords = new();
    public ICollection<SubscriptionUsage> UsageRecords => _usageRecords.AsReadOnly();

    private Subscription() { }

    public static Subscription Create(
        Guid planId,
        Guid userId,
        Guid? companyId,
        BillingCycle billingCycle,
        DateTime startDate,
        DateTime endDate,
        DateTime? trialEndDate)
    {
        var subscription = new Subscription
        {
            PlanId = planId,
            UserId = userId,
            CompanyId = companyId,
            BillingCycle = billingCycle,
            StartDate = startDate,
            EndDate = endDate,
            TrialEndDate = trialEndDate,
            IsAutoRenew = true,
            Status = trialEndDate.HasValue ? SubscriptionStatus.Trial : SubscriptionStatus.Active
        };

        subscription.AddDomainEvent(new SubscriptionCreatedEvent(subscription.Id, userId, planId, subscription.CreatedAt));
        return subscription;
    }

    public void Cancel(string reason)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SubscriptionCancelledEvent(Id, reason, CancelledAt.Value));
    }

    public void Upgrade(Guid newPlanId)
    {
        UpgradeFromPlanId = PlanId;
        PlanId = newPlanId;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SubscriptionUpgradedEvent(Id, UpgradeFromPlanId.Value, newPlanId, DateTime.UtcNow));
    }

    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Renew(DateTime newEndDate)
    {
        EndDate = newEndDate;
        Status = SubscriptionStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}
