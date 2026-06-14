using Document.Application.Interfaces;
using Document.Domain.Entities;
using Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Repositories;

public class InquiryStatsRepository : IInquiryStatsRepository
{
    private readonly DocumentDbContext _db;
    public InquiryStatsRepository(DocumentDbContext db) => _db = db;

    public async Task<int> CountByCompanyAsync(Guid companyId, DateTime? since = null, CancellationToken ct = default)
    {
        var query = _db.Inquiries.Where(x => x.CompanyId == companyId);
        if (since is not null) query = query.Where(x => x.CreatedAt >= since.Value);
        return await query.CountAsync(ct);
    }

    public async Task<List<Inquiry>> GetRecentByCompanyAsync(Guid companyId, int take, CancellationToken ct = default)
        => await _db.Inquiries
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<List<Inquiry>> GetAllByCompanyAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Inquiries
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
