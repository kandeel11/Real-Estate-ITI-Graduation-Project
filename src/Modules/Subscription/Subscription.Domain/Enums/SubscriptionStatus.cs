namespace Subscription.Domain.Enums;

public enum SubscriptionStatus
{
    /// <summary>
    /// المستخدم اختار خطة مدفوعة لكن لسه مفيش تأكيد دفع من Payment module
    /// </summary>
    PendingPayment,
    Trial,
    Active,
    Expired,
    Cancelled,
    Suspended
}
