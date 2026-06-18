using Microsoft.EntityFrameworkCore;
using Document.Infrastructure.Persistence;
using RealEstate.Shared.Kernel.Interfaces;

namespace Document.Infrastructure.Services;

public class LeaseService : ILeaseService
{
    private readonly DocumentDbContext _db;

    public LeaseService(DocumentDbContext db) => _db = db;

    public async Task<LeaseDto?> GetLeaseAsync(Guid leaseId)
    {
        var l = await _db.Leases
            .Where(x => x.Id == leaseId && x.DeletedAt == null)
            .FirstOrDefaultAsync();

        return l == null ? null : MapToDto(l);
    }

    public async Task<IEnumerable<LeaseDto>> GetLeasesByTenantAsync(Guid tenantId)
    {
        return await _db.Leases
            .Where(x => x.TenantId == tenantId && x.DeletedAt == null)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaseDto>> GetActiveLeasesAsync()
    {
        return await _db.Leases
            .Where(x => x.IsActive && x.DeletedAt == null)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    private static LeaseDto MapToDto(Domain.Entities.Lease l) => new(
        l.Id, l.PropertyId, l.TenantId, l.OwnerId,
        l.InstallmentAmount, l.PaymentDayOfMonth,
        l.StartDate, l.EndDate, l.IsActive);
}