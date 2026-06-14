namespace Document.Application.Interfaces;

/// <summary>
/// Read-only rating aggregations for dashboards. Separate from the rating
/// CRUD repositories so existing contracts stay untouched.
/// </summary>
public interface IRatingStatsRepository
{
    /// <summary>
    /// Star counts (1..5) for ratings belonging to a company — matched either
    /// by the rating's CompanyId or by one of the company's property ids.
    /// Rejected and soft-deleted ratings are excluded.
    /// </summary>
    Task<Dictionary<int, int>> GetCompanyDistributionAsync(
        Guid companyId, IReadOnlyCollection<Guid> propertyIds, CancellationToken ct = default);
}
