// AI.Application/Interfaces/IPropertyFilterService.cs

using AI.Application.DTOs;

namespace AI.Application.Interfaces;

/// <summary>
/// Implemented by Properties module.
/// Applies parsed filters to property database.
/// </summary>
public interface IPropertyFilterService
{
    Task<(IReadOnlyList<PropertySearchItem> Items, int Total)> SearchAsync(
        ParsedSearchFilters filters,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a specific property matches saved search filters.
    /// Used when new property is created.
    /// </summary>
    Task<bool> PropertyMatchesFiltersAsync(
        Guid propertyId,
        ParsedSearchFilters filters,
        CancellationToken ct = default);
}