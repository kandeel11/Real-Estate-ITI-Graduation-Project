using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

/// <summary>
/// Payment — الكيان المركزي لكل المعاملات المالية في النظام
/// (Subscription, Rent, Transfer, Refund, Deposit, Withdrawal)
///
/// Payment module هو "single source of truth" لكل عمليات الدفع.
/// التفاصيل الخاصة بكل نوع معاملة (Lease, Subscription, Transfer...) موجودة
/// في كيانات "Details" منفصلة (1:1) — RentPaymentDetails, SubscriptionPaymentDetails, TransferPaymentDetails.
/// </summary>
public class Payment
{
    public Guid Id { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "AED";

    public PaymentStatus Status { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public string? TransactionId { get; private set; }

    public string? CheckoutSessionId { get; private set; }

    public string? CheckoutUrl { get; private set; }

    public PaymentType PaymentType { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // ── Rent-specific bookkeeping (kept for backward compatibility with
    //    existing Rent flows: late fees, AI prediction, overdue checks) ──
    public DateOnly DueDate { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public decimal LateFee { get; private set; }

    public decimal CommissionAmount { get; private set; }

    public bool AiPredictedLate { get; private set; }

    public string? ReceiptUrl { get; private set; }

    public string? Notes { get; private set; }

    // ── Detail relations (1:1) — exactly one populated depending on PaymentType ──
    public RentPaymentDetails? RentDetails { get; private set; }

    public SubscriptionPaymentDetails? SubscriptionDetails { get; private set; }

    public TransferPaymentDetails? TransferDetails { get; private set; }

    private Payment() { }

    // ── Factory methods ──────────────────────────────────────────────

    /// <summary>
    /// ينشئ دفعة إيجار (Rent) مستحقة في تاريخ معين
    /// </summary>
    public static Payment CreateRent(Guid leaseId, Guid propertyId, Guid tenantId, Guid landlordId, decimal amount, DateOnly dueDate, string? notes = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = "AED",
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Other,
            PaymentType = PaymentType.Rent,
            DueDate = dueDate,
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        payment.RentDetails = RentPaymentDetails.Create(payment.Id, leaseId, propertyId, tenantId, landlordId);
        return payment;
    }

    /// <summary>
    /// ينشئ دفعة اشتراك (Subscription) بحالة Pending
    /// </summary>
    public static Payment CreateSubscription(
        Guid subscriptionId,
        Guid planId,
        BillingCycle billingCycle,
        decimal amount,
        string currency = "AED",
        string? notes = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Stripe,
            PaymentType = PaymentType.Subscription,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        payment.SubscriptionDetails = SubscriptionPaymentDetails.Create(
            payment.Id, subscriptionId, planId, billingCycle);

        return payment;
    }

    /// <summary>
    /// ينشئ معاملة تحويل أموال بين مستخدمين (Transfer)
    /// </summary>
    public static Payment CreateTransfer(Guid senderId, Guid receiverId, decimal amount, string purpose, string currency = "AED")
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Wallet,
            PaymentType = PaymentType.Transfer,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LateFee = 0,
            AiPredictedLate = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        payment.TransferDetails = TransferPaymentDetails.Create(payment.Id, senderId, receiverId, purpose);
        return payment;
    }

    /// <summary>
    /// ينشئ معاملة Deposit (إيداع)
    /// </summary>
    public static Payment CreateDeposit(decimal amount, string currency = "AED", string? notes = null)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentType = PaymentType.Deposit,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ينشئ معاملة Withdrawal (سحب)
    /// </summary>
    public static Payment CreateWithdrawal(decimal amount, string currency = "AED", string? notes = null)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentType = PaymentType.Withdrawal,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// ينشئ معاملة Refund مرتبطة بدفعة أصلية
    /// </summary>
    public static Payment CreateRefund(decimal amount, string currency, string reason)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = PaymentMethod.Stripe,
            PaymentType = PaymentType.Refund,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LateFee = 0,
            AiPredictedLate = false,
            Notes = reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ── Behaviors ────────────────────────────────────────────────────

    public void SetCheckoutSession(string sessionId, string checkoutUrl)
    {
        CheckoutSessionId = sessionId;
        CheckoutUrl = checkoutUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPaid(string transactionId, PaymentMethod paymentMethod, string? receiptUrl = null)
    {
        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        TransactionId = transactionId;
        PaymentMethod = paymentMethod;
        ReceiptUrl = receiptUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// نفس MarkPaid لكن بطريقة دفع نصية (متوافقة مع Stripe webhook الحالي)
    /// </summary>
    public void MarkAsPaid(string transactionId, string paymentMethod, string? receiptUrl = null)
    {
        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        TransactionId = transactionId;
        PaymentMethod = ParsePaymentMethod(paymentMethod);
        ReceiptUrl = receiptUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordCommission(decimal rate = 0.01m)
    {
        CommissionAmount = Math.Round(Amount * rate, 2);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyLateFee(decimal lateFeeAmount)
    {
        LateFee = lateFeeAmount;
        Status = PaymentStatus.Late;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAiPrediction(bool predictedLate)
    {
        AiPredictedLate = predictedLate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue(DateOnly today) =>
        Status == PaymentStatus.Pending && DueDate < today && PaymentType == PaymentType.Rent;

    public void MarkAsRefunded(string reason)
    {
        Status = PaymentStatus.Refunded;
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"Refunded: {reason}"
            : $"{Notes} | Refunded: {reason}";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// مفتاح وصول مختصر إلى LeaseId — موجود فقط لو PaymentType=Rent
    /// </summary>
    public Guid LeaseId => RentDetails?.LeaseId ?? Guid.Empty;

    private static PaymentMethod ParsePaymentMethod(string method) =>
        method.ToLowerInvariant() switch
        {
            "card" => PaymentMethod.Card,
            "cash" => PaymentMethod.Cash,
            "banktransfer" or "bank_transfer" => PaymentMethod.BankTransfer,
            "wallet" => PaymentMethod.Wallet,
            "stripe" => PaymentMethod.Stripe,
            _ => PaymentMethod.Other
        };
}
