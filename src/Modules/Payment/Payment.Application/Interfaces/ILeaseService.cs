namespace Payment.Application.Interfaces;

/// <summary>
/// Contract للتواصل مع الـ Lease Module
/// الـ Payment Module لا يملك Lease entities — بيستخدم الـ interface دي فقط
/// </summary>
public interface ILeaseService
{
    /// <summary>
    /// جيب كل الـ leases الأكتيف علشان نعمل لها invoices شهرية
    /// </summary>
    Task<IReadOnlyList<ActiveLeaseDto>> GetActiveLeasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// جيب بيانات lease معين بالـ ID
    /// </summary>
    Task<ActiveLeaseDto?> GetLeaseByIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// البيانات اللي Payment module محتاجها من الـ Lease
/// PropertyId/TenantId/LandlordId مطلوبين لبناء RentPaymentDetails
/// </summary>
public sealed record ActiveLeaseDto(
    Guid LeaseId,
    decimal InstallmentAmount,
    int PaymentDayOfMonth,
    string TenantName,
    string TenantEmail,
    string? TenantPhone,
    Guid PropertyId = default,
    Guid TenantId = default,
    Guid LandlordId = default
);
