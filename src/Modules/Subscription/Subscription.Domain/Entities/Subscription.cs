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

    // ملاحظة: Subscription module مش بيخزن أي معلومات عن الدفع —
    // Payment module هو single source of truth لكل المعاملات المالية.
    // الربط بين Subscription والـ Payment بيتم فقط عبر Integration Events
    // (SubscriptionPaymentSucceededEvent) — لا يوجد FK أو navigation هنا.

    private readonly List<SubscriptionUsage> _usageRecords = new();
    public ICollection<SubscriptionUsage> UsageRecords => _usageRecords.AsReadOnly();

    private Subscription() { }

    /// <summary>
    /// ينشئ Subscription بحالة Active مباشرة — يُستخدم للخطط المجانية
    /// (Free tier auto-provisioning عند إنشاء Company/IndividualUser)
    /// حيث لا يوجد دفع مطلوب.
    /// </summary>
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

    /// <summary>
    /// ينشئ Subscription بحالة PendingPayment — يُستخدم عند اختيار المستخدم لخطة مدفوعة.
    /// الـ Subscription يفضل Pending لحد ما Payment module يأكد نجاح الدفع
    /// عبر SubscriptionPaymentSucceededEvent، وقتها بنستخدم Activate().
    /// </summary>
    public static Subscription CreatePendingPayment(
        Guid planId,
        Guid userId,
        Guid? companyId,
        BillingCycle billingCycle)
    {
        var subscription = new Subscription
        {
            PlanId = planId,
            UserId = userId,
            CompanyId = companyId,
            BillingCycle = billingCycle,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow, // سيتم تحديدها فعلياً في Activate()
            TrialEndDate = null,
            IsAutoRenew = true,
            Status = SubscriptionStatus.PendingPayment
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

    /// <summary>
    /// يفعّل الاشتراك بعد تأكيد نجاح الدفع من Payment module
    /// (استدعاؤها من SubscriptionPaymentSucceededEventHandler)
    /// </summary>
    public void Activate(DateTime startDate, DateTime endDate)
    {
        if (Status != SubscriptionStatus.PendingPayment)
            throw new InvalidOperationException(
                $"Cannot activate subscription {Id}: current status is '{Status}', expected '{SubscriptionStatus.PendingPayment}'.");

        Status = SubscriptionStatus.Active;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }
}
