using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

/// <summary>
/// تفاصيل دفعة الاشتراك — relation 1:1 مع Payment
/// بدون أي FK لجداول Subscription module (modular boundary)
/// </summary>
public class SubscriptionPaymentDetails
{
    public Guid PaymentId { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public Guid PlanId { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    private SubscriptionPaymentDetails() { }

    public static SubscriptionPaymentDetails Create(
        Guid paymentId,
        Guid subscriptionId,
        Guid planId,
        BillingCycle billingCycle)
    {
        return new SubscriptionPaymentDetails
        {
            PaymentId = paymentId,
            SubscriptionId = subscriptionId,
            PlanId = planId,
            BillingCycle = billingCycle
        };
    }
}
