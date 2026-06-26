using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record ManagementLeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    Guid? BrokerId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    string? ManagerApprovalStatus,
    string? ManagerNotes,
    DateTime? ManagerApprovedAt,
    bool IsFullManagement,
    string? ManagementOptions,
    string ExtractionStatus,
    DateTime CreatedAt,
    string? ContractFileUrl
);

public record ActiveManagementLeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    Guid? BrokerId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsFullManagement,
    string? ManagementOptions,
    DateTime? TenantConfirmedAt,
    DateTime CreatedAt
);

/// <summary>Active leases under the company's properties (tenant accepted).</summary>
public record GetActiveCompanyLeasesQuery(List<Guid> PropertyIds) : IRequest<List<ActiveManagementLeaseDto>>;

public class GetActiveCompanyLeasesHandler(IDocumentDbContext db)
    : IRequestHandler<GetActiveCompanyLeasesQuery, List<ActiveManagementLeaseDto>>
{
    public async Task<List<ActiveManagementLeaseDto>> Handle(GetActiveCompanyLeasesQuery q, CancellationToken ct)
    {
        if (q.PropertyIds.Count == 0) return [];

        var leases = await db.Leases
            .Where(l =>
                q.PropertyIds.Contains(l.PropertyId) &&
                l.DeletedAt == null &&
                l.IsActive)
            .OrderByDescending(l => l.TenantConfirmedAt)
            .ToListAsync(ct);

        return leases.Select(l => new ActiveManagementLeaseDto(
            l.Id, l.PropertyId, l.TenantId, l.OwnerId, l.BrokerId,
            l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
            l.SecurityDeposit, l.StartDate, l.EndDate,
            l.IsFullManagement, l.ManagementOptions, l.TenantConfirmedAt, l.CreatedAt
        )).ToList();
    }
}

/// <summary>
/// Leases under the company's properties that are awaiting manager approval.
/// ManagerApprovalStatus = "PendingApproval", not yet active.
/// </summary>
public record GetPendingApprovalLeasesQuery(List<Guid> PropertyIds) : IRequest<List<ManagementLeaseDto>>;

public class GetPendingApprovalLeasesHandler(IDocumentDbContext db)
    : IRequestHandler<GetPendingApprovalLeasesQuery, List<ManagementLeaseDto>>
{
    public async Task<List<ManagementLeaseDto>> Handle(GetPendingApprovalLeasesQuery q, CancellationToken ct)
    {
        if (q.PropertyIds.Count == 0) return [];

        var leases = await db.Leases
            .Where(l =>
                q.PropertyIds.Contains(l.PropertyId) &&
                l.DeletedAt == null &&
                !l.IsActive &&
                l.ManagerApprovalStatus == "PendingApproval")
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return leases.Select(l => new ManagementLeaseDto(
            l.Id, l.PropertyId, l.TenantId, l.OwnerId, l.BrokerId,
            l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
            l.SecurityDeposit, l.StartDate, l.EndDate, l.PaymentDayOfMonth,
            l.ManagerApprovalStatus, l.ManagerNotes, l.ManagerApprovedAt,
            l.IsFullManagement, l.ManagementOptions, l.ExtractionStatus, l.CreatedAt,
            l.ContractFileUrl
        )).ToList();
    }
}

/// <summary>
/// Leases under the company's properties that the manager has approved and are now
/// waiting for the tenant to accept. ManagerApprovalStatus = "Approved", ExtractionStatus = "extracted".
/// </summary>
public record GetAwaitingTenantLeasesQuery(List<Guid> PropertyIds) : IRequest<List<ManagementLeaseDto>>;

public class GetAwaitingTenantLeasesHandler(IDocumentDbContext db)
    : IRequestHandler<GetAwaitingTenantLeasesQuery, List<ManagementLeaseDto>>
{
    public async Task<List<ManagementLeaseDto>> Handle(GetAwaitingTenantLeasesQuery q, CancellationToken ct)
    {
        if (q.PropertyIds.Count == 0) return [];

        var leases = await db.Leases
            .Where(l =>
                q.PropertyIds.Contains(l.PropertyId) &&
                l.DeletedAt == null &&
                !l.IsActive &&
                l.ManagerApprovalStatus == "Approved" &&
                l.ExtractionStatus == "extracted")
            .OrderByDescending(l => l.ManagerApprovedAt)
            .ToListAsync(ct);

        return leases.Select(l => new ManagementLeaseDto(
            l.Id, l.PropertyId, l.TenantId, l.OwnerId, l.BrokerId,
            l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
            l.SecurityDeposit, l.StartDate, l.EndDate, l.PaymentDayOfMonth,
            l.ManagerApprovalStatus, l.ManagerNotes, l.ManagerApprovedAt,
            l.IsFullManagement, l.ManagementOptions, l.ExtractionStatus, l.CreatedAt,
            l.ContractFileUrl
        )).ToList();
    }
}
