using Property.Application.DTOs;
using Property.Domain.Entities;

namespace Property.Application.Interfaces;

public interface IPropertyAnalyticsRepository
{
    Task AddAsync(PropertyAnalytics analytics, CancellationToken ct = default);
    Task<AnalyticsDto> GetAnalyticsAsync(Guid propertyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
