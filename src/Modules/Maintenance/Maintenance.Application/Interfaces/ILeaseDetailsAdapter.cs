using Maintenance.Application.DTOs.Responses;

namespace Maintenance.Application.Interfaces;

/// <summary>
/// Cross-module port: Maintenance.Application asks for lease contract details without
/// depending directly on the Document module. The Host layer wires up the concrete adapter.
/// </summary>
public interface ILeaseDetailsAdapter
{
    /// <summary>
    /// Returns a summary of the lease contract relevant to dispute resolution.
    /// Returns <c>null</c> if the lease does not exist or has been deleted.
    /// </summary>
    Task<LeaseContractSummaryDto?> GetLeaseContractAsync(Guid leaseId, CancellationToken ct = default);
}
