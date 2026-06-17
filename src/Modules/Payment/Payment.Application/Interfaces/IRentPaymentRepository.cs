using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

/// <summary>
/// الـ Repository interface بتاع RentPayment
/// Application layer بيعرف بس الـ interface — مش الـ implementation
/// </summary>
public interface IRentPaymentRepository
{
    Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPayment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPayment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// جيب الدفعات المتأخرة: Status=Pending و DueDate < اليوم
    /// </summary>
    Task<IReadOnlyList<RentPayment>> GetOverdueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// هل فيه دفعة موجودة للـ lease ده في الشهر ده؟ (لتجنب التكرار)
    /// </summary>
    Task<bool> ExistsForLeaseAndMonthAsync(Guid leaseId, DateOnly dueDate, CancellationToken cancellationToken = default);

    Task AddAsync(RentPayment payment, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<RentPayment> payments, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
