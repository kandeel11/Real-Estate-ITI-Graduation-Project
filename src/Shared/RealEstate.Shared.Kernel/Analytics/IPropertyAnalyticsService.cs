namespace RealEstate.Shared.Kernel.Analytics;

/// <summary>
/// Implemented by Property.Infrastructure. Allows the AI module to query property
/// analytics without taking a direct dependency on PropertyDbContext.
/// </summary>
public interface IPropertyAnalyticsService
{
    Task<PropertyAnalyticsSummaryDto> GetSummaryAsync(CancellationToken ct = default);
    Task<int> GetNewListingsCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<CityListingCountDto>> GetTopCitiesAsync(int topN = 5, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyTypeCountDto>> GetCountsByTypeAsync(CancellationToken ct = default);
}

public record PropertyAnalyticsSummaryDto(
    int Total,
    int Active,
    int Rented,
    int PendingVerification,
    int Rejected,
    int TotalActiveLeases);

public record CityListingCountDto(string City, int Count);

public record PropertyTypeCountDto(string PropertyType, int Count);
