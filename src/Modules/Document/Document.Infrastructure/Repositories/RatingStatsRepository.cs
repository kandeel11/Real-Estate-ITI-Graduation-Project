using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Repositories;

public class RatingStatsRepository : IRatingStatsRepository
{
    private readonly DocumentDbContext _db;
    public RatingStatsRepository(DocumentDbContext db) => _db = db;

    public async Task<Dictionary<int, int>> GetCompanyDistributionAsync(
        Guid companyId, IReadOnlyCollection<Guid> propertyIds, CancellationToken ct = default)
    {
        var buckets = await _db.Ratings
            .Where(r => r.DeletedAt == null
                        && r.ModerationStatus != ReviewModerationStatus.Rejected
                        && (r.CompanyId == companyId
                            || (r.PropertyId != null && propertyIds.Contains(r.PropertyId.Value))))
            .GroupBy(r => r.RatingValue)
            .Select(g => new { Stars = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return buckets.ToDictionary(b => b.Stars, b => b.Count);
    }
}
