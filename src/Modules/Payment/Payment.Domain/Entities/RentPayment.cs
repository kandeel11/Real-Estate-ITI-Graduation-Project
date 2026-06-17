using Payment.Domain.Enums;

namespace Payment.Domain.Entities;
public class RentPayment
{
    public Guid Id { get; private set; }
    public Guid LeaseId { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly DueDate { get; private set; }

    public DateTime? PaidAt { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? PaymentMethod { get; private set; }

    public string? TransactionId { get; private set; }

    public decimal LateFee { get; private set; }

    public bool AiPredictedLate { get; private set; }

    public string? ReceiptUrl { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
    public PaymentType PaymentType { get; private set; } = PaymentType.Rent;

    /// <summary>
    /// Stripe Checkout Session ID — مستخدم بس مع Subscription
    /// </summary>
    public string? CheckoutSessionId { get; private set; }

    /// <summary>
    /// Stripe Checkout Session URL — يتم تحويل المستخدم له للدفع
    /// </summary>
    public string? CheckoutUrl { get; private set; }

    /// <summary>
    /// تفاصيل دفعة الاشتراك — موجودة بس لما PaymentType = Subscription
    /// </summary>
    public SubscriptionPaymentDetails? SubscriptionDetails { get; private set; }

    private RentPayment() { }

    public static RentPayment Create(Guid leaseId, decimal amount, DateOnly dueDate, string? notes = null)
    {
        return new RentPayment
        {
            Id = Guid.NewGuid(),
            LeaseId = leaseId,
            Amount = amount,
            DueDate = dueDate,
            Status = PaymentStatus.Pending,
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    public static RentPayment CreateSubscriptionPayment(decimal amount, string? notes = null)
    {
        return new RentPayment
        {
            Id = Guid.NewGuid(),
            LeaseId = Guid.Empty, // غير مستخدم لـ Subscription
            Amount = amount,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = PaymentStatus.Pending,
            PaymentType = PaymentType.Subscription,
            LateFee = 0,
            AiPredictedLate = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// يربط تفاصيل الاشتراك بهذه الدفعة — مسموح بس لو PaymentType=Subscription
    /// </summary>
    public void AttachSubscriptionDetails(SubscriptionPaymentDetails details)
    {
        if (PaymentType != PaymentType.Subscription)
            throw new InvalidOperationException("Cannot attach subscription details to a non-subscription payment.");

        SubscriptionDetails = details;
        UpdatedAt = DateTime.UtcNow;
    }
    /// <summary>
    /// يخزن بيانات Stripe Checkout Session على الدفعة
    /// </summary>
    public void SetCheckoutSession(string sessionId, string checkoutUrl)
    {
        CheckoutSessionId = sessionId;
        CheckoutUrl = checkoutUrl;
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkAsPaid(string transactionId, string paymentMethod, string? receiptUrl = null)
    {
        Status = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        TransactionId = transactionId;
        PaymentMethod = paymentMethod;
        ReceiptUrl = receiptUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
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
        Status == PaymentStatus.Pending && DueDate < today;
    public void MarkAsRefunded(string reason)
    {
        Status = PaymentStatus.Refunded;
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"Refunded: {reason}"
            : $"{Notes} | Refunded: {reason}";
        UpdatedAt = DateTime.UtcNow;
    }
}