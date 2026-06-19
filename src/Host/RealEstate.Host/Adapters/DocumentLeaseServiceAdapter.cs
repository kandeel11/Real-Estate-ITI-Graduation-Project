using Document.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges the Document module's Lease table into the Payment module's ILeaseService contract.
/// Replaces MockLeaseService — registered before AddPaymentModule() so TryAddScoped is a no-op.
/// </summary>
public sealed class DocumentLeaseServiceAdapter : ILeaseService
{
    private readonly DocumentDbContext _documentDb;
    private readonly IdentityDbContext _identityDb;

    public DocumentLeaseServiceAdapter(DocumentDbContext documentDb, IdentityDbContext identityDb)
    {
        _documentDb = documentDb;
        _identityDb = identityDb;
    }

    public async Task<IReadOnlyList<ActiveLeaseDto>> GetActiveLeasesAsync(CancellationToken cancellationToken = default)
    {
        var leases = await _documentDb.Leases
            .Where(l => l.IsActive && l.DeletedAt == null && l.InstallmentAmount != null && l.PaymentDayOfMonth != null)
            .ToListAsync(cancellationToken);

        if (leases.Count == 0)
            return Array.Empty<ActiveLeaseDto>();

        var tenantIds = leases.Select(l => l.TenantId).Distinct().ToList();
        var tenants = await _identityDb.Users
            .Where(u => tenantIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email, u.Phone })
            .ToListAsync(cancellationToken);

        var tenantMap = tenants.ToDictionary(t => t.Id);

        return leases.Select(l =>
        {
            tenantMap.TryGetValue(l.TenantId, out var tenant);
            return new ActiveLeaseDto(
                LeaseId: l.Id,
                InstallmentAmount: l.InstallmentAmount!.Value,
                PaymentDayOfMonth: l.PaymentDayOfMonth!.Value,
                TenantName: tenant?.FullName ?? "Unknown",
                TenantEmail: tenant?.Email ?? string.Empty,
                TenantPhone: tenant?.Phone,
                PropertyId: l.PropertyId,
                TenantId: l.TenantId,
                LandlordId: l.OwnerId,
                StartDate: l.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate: l.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
            );
        }).ToList();
    }

    public async Task<ActiveLeaseDto?> GetLeaseByIdAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        var lease = await _documentDb.Leases
            .Where(l => l.Id == leaseId && l.IsActive && l.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (lease is null)
            return null;

        var tenant = await _identityDb.Users
            .Where(u => u.Id == lease.TenantId)
            .Select(u => new { u.FullName, u.Email, u.Phone })
            .FirstOrDefaultAsync(cancellationToken);

        return new ActiveLeaseDto(
            LeaseId: lease.Id,
            InstallmentAmount: lease.InstallmentAmount ?? 0m,
            PaymentDayOfMonth: lease.PaymentDayOfMonth ?? 1,
            TenantName: tenant?.FullName ?? "Unknown",
            TenantEmail: tenant?.Email ?? string.Empty,
            TenantPhone: tenant?.Phone,
            PropertyId: lease.PropertyId,
            TenantId: lease.TenantId,
            LandlordId: lease.OwnerId,
            StartDate: lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate: lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        );
    }
}
