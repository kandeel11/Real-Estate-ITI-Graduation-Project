namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Paid,
    Late,
    Failed,
    Refunded,
    /// <summary>
    /// First installment was paid in cash/real-world before the lease was registered
    /// on the platform.  The admin marks this manually, or the system sets it
    /// automatically when a lease activates and the first due date is already in the past.
    /// These payments are excluded from the Overdue queue and cannot receive a late fee.
    /// </summary>
    PaidExternally
}