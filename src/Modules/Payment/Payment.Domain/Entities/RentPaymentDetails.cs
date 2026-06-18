namespace Payment.Domain.Entities;

/// <summary>
/// تفاصيل دفعة الإيجار — relation 1:1 مع Payment
/// بدون أي FK لجداول Property/Identity module (modular boundary)
/// </summary>
public class RentPaymentDetails
{
    public Guid PaymentId { get; private set; }

    public Guid LeaseId { get; private set; }

    public Guid PropertyId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid LandlordId { get; private set; }

    private RentPaymentDetails() { }

    public static RentPaymentDetails Create(
        Guid paymentId,
        Guid leaseId,
        Guid propertyId,
        Guid tenantId,
        Guid landlordId)
    {
        return new RentPaymentDetails
        {
            PaymentId = paymentId,
            LeaseId = leaseId,
            PropertyId = propertyId,
            TenantId = tenantId,
            LandlordId = landlordId
        };
    }
}
