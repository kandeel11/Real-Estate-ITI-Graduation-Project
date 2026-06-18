namespace Payment.Domain.Enums;

/// <summary>
/// طريقة الدفع المستخدمة في المعاملة
/// </summary>
public enum PaymentMethod
{
    Card = 0,
    Cash = 1,
    BankTransfer = 2,
    Wallet = 3,
    Stripe = 4,
    Other = 5
}
