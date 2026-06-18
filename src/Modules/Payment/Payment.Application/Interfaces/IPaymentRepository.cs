using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

/// <summary>
/// الـ Repository interface بتاع Payment — single source of truth لكل المعاملات المالية
/// Application layer بيعرف بس الـ interface — مش الـ implementation
/// </summary>
public interface IPaymentRepository
{
    Task<Payment.Domain.Entities.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// جيب الدفعات المتأخرة: Status=Pending و DueDate < اليوم (Rent فقط)
    /// </summary>
    Task<IReadOnlyList<Payment.Domain.Entities.Payment>> GetOverdueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// هل فيه دفعة موجودة للـ lease ده في الشهر ده؟ (لتجنب التكرار)
    /// </summary>
    Task<bool> ExistsForLeaseAndMonthAsync(Guid leaseId, DateOnly dueDate, CancellationToken cancellationToken = default);

    Task AddAsync(Payment.Domain.Entities.Payment payment, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Payment.Domain.Entities.Payment> payments, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
