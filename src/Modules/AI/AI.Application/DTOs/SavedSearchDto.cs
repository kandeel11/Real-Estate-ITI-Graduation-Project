// AI.Application/DTOs/SavedSearchDto.cs

namespace AI.Application.DTOs;

public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    string RawQuery,
    ParsedSearchFilters ParsedFilters,
    bool NotifyOnMatch,
    bool IsActive,
    DateTime CreatedAt
);