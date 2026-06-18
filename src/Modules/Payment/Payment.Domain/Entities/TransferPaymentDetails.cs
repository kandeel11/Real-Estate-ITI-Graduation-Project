namespace Payment.Domain.Entities;

/// <summary>
/// تفاصيل معاملة تحويل الأموال — relation 1:1 مع Payment
/// </summary>
public class TransferPaymentDetails
{
    public Guid PaymentId { get; private set; }

    public Guid SenderId { get; private set; }

    public Guid ReceiverId { get; private set; }

    public string Purpose { get; private set; } = string.Empty;

    private TransferPaymentDetails() { }

    public static TransferPaymentDetails Create(
        Guid paymentId,
        Guid senderId,
        Guid receiverId,
        string purpose)
    {
        return new TransferPaymentDetails
        {
            PaymentId = paymentId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Purpose = purpose
        };
    }
}
