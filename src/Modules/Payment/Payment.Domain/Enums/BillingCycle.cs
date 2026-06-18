namespace Payment.Domain.Enums;

/// <summary>
/// دورة الفوترة — لازم تتطابق مع Subscription.Domain.Enums.BillingCycle
/// عشان الـ Integration Event يفضل consistent بين الـ modules
/// </summary>
public enum BillingCycle
{
    Monthly = 0,
    Quarterly = 1,
    Annual = 2
}
