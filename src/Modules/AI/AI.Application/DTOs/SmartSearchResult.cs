// AI.Application/DTOs/SmartSearchResult.cs

namespace AI.Application.DTOs;

public sealed record SmartSearchResult(
    Guid QueryId,          // for saving this search
    ParsedSearchFilters ParsedQuery,      // shown to user as "We understood..."
    int TotalResults,
    IReadOnlyList<PropertySearchItem> Properties,
    int Page,
    int PageSize,
    int TokensUsed
);

public sealed record PropertySearchItem(
    Guid PropertyId,
    string Title,
    string PropertyType,
    string City,
    string District,
    double AreaInMeters,
    int? Bedrooms,
    int? Bathrooms,
    decimal Price,
    string ListingType,
    string? Description,
    string? MainImageUrl,   // SAS URL
    double MatchScore      // how well it matches the query
);