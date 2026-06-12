using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IRentPaymentRepository
{
    Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPayment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPayment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPayment>> GetOverdueAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsForLeaseAndMonthAsync(Guid leaseId, DateOnly dueDate, CancellationToken cancellationToken = default);

    Task AddAsync(RentPayment payment, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<RentPayment> payments, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
