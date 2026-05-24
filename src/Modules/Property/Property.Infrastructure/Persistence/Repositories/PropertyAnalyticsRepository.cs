using Microsoft.EntityFrameworkCore;
using Property.Application.DTOs;
using Property.Application.Interfaces;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence.Contexts;

namespace Property.Infrastructure.Persistence.Repositories;

public class PropertyAnalyticsRepository : IPropertyAnalyticsRepository
{
    private readonly PropertyDbContext _context;

    public PropertyAnalyticsRepository(PropertyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PropertyAnalytics analytics, CancellationToken ct = default)
        => await _context.PropertyAnalytics.AddAsync(analytics, ct);

    public async Task<AnalyticsDto> GetAnalyticsAsync(Guid propertyId, CancellationToken ct = default)
    {
        var records = await _context.PropertyAnalytics
            .Where(a => a.PropertyId == propertyId)
            .ToListAsync(ct);

        var viewsByDay = records
            .GroupBy(a => DateOnly.FromDateTime(a.ViewedAt))
            .Select(g => new DailyViewDto { Date = g.Key, Views = g.Count() })
            .OrderBy(d => d.Date)
            .ToList();

        var sourceBreakdown = records
            .Where(a => a.Source.HasValue)
            .GroupBy(a => a.Source!.Value.ToString())
            .Select(g => new SourceBreakdownDto { Source = g.Key, Views = g.Count() })
            .ToList();

        return new AnalyticsDto
        {
            PropertyId = propertyId,
            TotalViews = records.Count,
            ViewsByDay = viewsByDay,
            SourceBreakdown = sourceBreakdown
        };
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
