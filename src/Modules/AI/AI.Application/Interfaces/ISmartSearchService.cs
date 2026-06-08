// AI.Application/Interfaces/ISmartSearchService.cs

using AI.Application.DTOs;

namespace AI.Application.Interfaces;

public interface ISmartSearchService
{
    /// <summary>
    /// Parses natural language query into structured property filters.
    /// Supports Arabic, English, and mixed queries.
    /// </summary>
    Task<ParsedSearchFilters> ParseQueryAsync(
        string rawQuery,
        CancellationToken cancellationToken = default);
}