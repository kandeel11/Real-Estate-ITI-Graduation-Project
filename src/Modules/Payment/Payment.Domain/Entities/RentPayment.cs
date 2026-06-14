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

    public void MarkAsRefunded(string? reason = null)
    {
        if (Status != PaymentStatus.Paid)
            throw new InvalidOperationException("Only paid payments can be refunded.");

        Status = PaymentStatus.Refunded;

        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? $"Refund reason: {reason}"
                : $"{Notes} | Refund reason: {reason}";

        UpdatedAt = DateTime.UtcNow;
    }
}