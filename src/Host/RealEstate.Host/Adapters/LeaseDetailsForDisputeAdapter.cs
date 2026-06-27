using Document.Infrastructure.Persistence;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges the Document module's Lease table into the Maintenance module's
/// ILeaseDetailsAdapter contract for the Ticket Dispute workflow.
/// Registered in Program.cs before AddMaintenanceModule resolves its handlers.
/// </summary>
public sealed class LeaseDetailsForDisputeAdapter(DocumentDbContext documentDb) : ILeaseDetailsAdapter
{
    public async Task<LeaseContractSummaryDto?> GetLeaseContractAsync(Guid leaseId, CancellationToken ct = default)
    {
        var lease = await documentDb.Leases
            .Where(l => l.Id == leaseId && l.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (lease is null)
            return null;

        return new LeaseContractSummaryDto(
            LeaseId: lease.Id,
            MonthlyInstallment: lease.InstallmentAmount,
            TotalRentAmount: lease.TotalRentAmount,
            SecurityDeposit: lease.SecurityDeposit,
            StartDate: lease.StartDate,
            EndDate: lease.EndDate,
            DurationInMonths: lease.DurationInMonths,
            PaymentFrequency: lease.PaymentFrequency?.ToString(),
            ContractFileUrl: lease.ContractFileUrl,
            IsFullManagement: lease.IsFullManagement,
            ManagementOptions: lease.ManagementOptions
        );
    }
}
