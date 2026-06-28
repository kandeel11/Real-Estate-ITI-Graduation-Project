namespace Maintenance.Application.DTOs.Responses;

/// <summary>
/// Lease contract data surfaced to the Admin during dispute resolution.
/// Fetched cross-module via ILeaseDetailsAdapter — the Maintenance module never
/// references the Document module directly.
/// </summary>
public record LeaseContractSummaryDto(
    Guid LeaseId,
    decimal? MonthlyInstallment,
    decimal? TotalRentAmount,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? DurationInMonths,
    string? PaymentFrequency,
    /// <summary>Direct URL to the signed PDF contract stored in Azure Blob.</summary>
    string? ContractFileUrl,
    bool IsFullManagement,
    /// <summary>
    /// Free-text maintenance/management options agreed in the contract
    /// (maps to Lease.ManagementOptions — captures maintenance liability terms).
    /// </summary>
    string? ManagementOptions
);
