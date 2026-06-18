namespace Payment.Domain.Enums;

/// <summary>
/// نوع الدفعة — Payment module هو المصدر الوحيد للحقيقة لكل أنواع المعاملات المالية
/// </summary>
public enum PaymentType
{
    Subscription = 0,
    Rent = 1,
    Transfer = 2,
    Refund = 3,
    Deposit = 4,
    Withdrawal = 5
}
