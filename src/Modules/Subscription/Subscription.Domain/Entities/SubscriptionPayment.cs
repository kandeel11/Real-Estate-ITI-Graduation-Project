using Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Domain.Events;

namespace Subscription.Domain.Entities;

public class SubscriptionPayment : BaseEntity
{
    public Guid SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? TransactionId { get; private set; }
    public SubscriptionPaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime DueDate { get; private set; }
    public string? InvoiceUrl { get; private set; }

    public Subscription Subscription { get; private set; } = null!;

    private SubscriptionPayment() { }

    public static SubscriptionPayment Create(
        Guid subscriptionId,
        decimal amount,
        string currency,
        string paymentMethod,
        DateTime dueDate)
    {
        return new SubscriptionPayment
        {
            SubscriptionId = subscriptionId,
            Amount = amount,
            Currency = currency,
            PaymentMethod = paymentMethod,
            DueDate = dueDate,
            Status = SubscriptionPaymentStatus.Pending
        };
    }

    public void MarkPaid(string transactionId, DateTime paidAt, string? invoiceUrl = null)
    {
        TransactionId = transactionId;
        PaidAt = paidAt;
        InvoiceUrl = invoiceUrl;
        Status = SubscriptionPaymentStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new SubscriptionPaymentProcessedEvent(Id, SubscriptionId, Amount, paidAt));
    }

    public void MarkFailed()
    {
        Status = SubscriptionPaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = SubscriptionPaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
