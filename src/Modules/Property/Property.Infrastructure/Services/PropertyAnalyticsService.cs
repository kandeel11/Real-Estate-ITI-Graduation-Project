using Microsoft.EntityFrameworkCore;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence.Contexts;
using RealEstate.Shared.Kernel.Analytics;

namespace Property.Infrastructure.Services;

public sealed class PropertyAnalyticsService : IPropertyAnalyticsService
{
    private readonly PropertyDbContext _db;

    public PropertyAnalyticsService(PropertyDbContext db) => _db = db;

    public async Task<PropertyAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var q = _db.Properties.AsNoTracking();

        var total               = await q.CountAsync(ct);
        var active              = await q.CountAsync(p => p.Status == PropertyStatus.Active, ct);
        var rented              = await q.CountAsync(p => p.Status == PropertyStatus.Rented, ct);
        var pendingVerification = await q.CountAsync(p => p.Status == PropertyStatus.PendingReview, ct);
        var rejected            = await q.CountAsync(p => p.Status == PropertyStatus.Rejected, ct);

        // Rented properties are a 1-to-1 proxy for active leases within this module's boundary.
        return new PropertyAnalyticsSummaryDto(total, active, rented, pendingVerification, rejected, rented);
    }

    public async Task<int> GetNewListingsCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.Properties
            .AsNoTracking()
            .CountAsync(p => p.CreatedAt >= from && p.CreatedAt <= to, ct);

    public async Task<IReadOnlyList<CityListingCountDto>> GetTopCitiesAsync(int topN = 5, CancellationToken ct = default)
    {
        var rows = await _db.Properties
            .AsNoTracking()
            .Where(p => p.City != null)
            .GroupBy(p => p.City)
            .Select(g => new { City = g.Key!, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToListAsync(ct);

        return rows.Select(x => new CityListingCountDto(x.City, x.Count)).ToList();
    }

    public async Task<IReadOnlyList<PropertyTypeCountDto>> GetCountsByTypeAsync(CancellationToken ct = default)
    {
        var rows = await _db.Properties
            .AsNoTracking()
            .GroupBy(p => p.PropertyType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .Select(x => new PropertyTypeCountDto(x.Type.ToString()!, x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();
    }
}
