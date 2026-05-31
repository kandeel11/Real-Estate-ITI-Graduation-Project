using Document.Application.Interfaces;
using Document.Domain.Entities;
using Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Repositories;

public class InquiryRepository : IInquiryRepository
{
    private readonly DocumentDbContext _db;
    public InquiryRepository(DocumentDbContext db) => _db = db;

    public async Task<Domain.Entities.Inquiry?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Inquiries.Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<Domain.Entities.Inquiry>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct)
        => await _db.Inquiries.Where(x => x.PropertyId == propertyId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<List<Domain.Entities.Inquiry>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
        => await _db.Inquiries.Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<List<Domain.Entities.Inquiry>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct)
        => await _db.Inquiries.Where(x => x.OwnerId == ownerId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Inquiry inquiry, CancellationToken ct)
    {
        await _db.Inquiries.AddAsync(inquiry, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Domain.Entities.Inquiry inquiry, CancellationToken ct)
    {
        _db.Inquiries.Update(inquiry);
        await _db.SaveChangesAsync(ct);
    }
}