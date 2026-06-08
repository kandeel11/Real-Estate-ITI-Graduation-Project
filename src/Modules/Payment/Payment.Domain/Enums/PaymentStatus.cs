namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Paid,
    Late,
    Failed,
    Refunded
}