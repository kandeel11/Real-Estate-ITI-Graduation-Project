using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface ILeaseService
{
    Task<IReadOnlyList<ActiveLeaseDto>> GetActiveLeasesAsync(CancellationToken cancellationToken = default);
    Task<ActiveLeaseDto?> GetLeaseByIdAsync(Guid leaseId, CancellationToken cancellationToken = default);
}
