namespace Payment.Domain.Enums;

/// <summary>
/// سبب دفعة الاشتراك — جديد، تجديد، أو ترقية
/// </summary>
public enum SubscriptionPaymentPurpose
{
    NewSubscription = 0,
    Renewal = 1,
    Upgrade = 2
}
