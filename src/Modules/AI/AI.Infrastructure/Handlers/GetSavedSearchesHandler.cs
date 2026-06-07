// AI.Infrastructure/Handlers/GetSavedSearchesHandler.cs

using AI.Application.DTOs;
using AI.Application.Interfaces;
using AI.Application.Queries.GetSavedSearches;
using MediatR;
using System.Text.Json;

namespace AI.Infrastructure.Handlers;

public sealed class GetSavedSearchesHandler
    : IRequestHandler<GetSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    private readonly ISavedSearchRepository _repo;

    public GetSavedSearchesHandler(ISavedSearchRepository repo)
        => _repo = repo;

    public async Task<IReadOnlyList<SavedSearchDto>> Handle(
        GetSavedSearchesQuery query,
        CancellationToken ct)
    {
        var searches = await _repo.GetByUserIdAsync(query.UserId, ct);

        return searches.Select(s =>
        {
            var filters = JsonSerializer
                .Deserialize<ParsedSearchFilters>(s.ParsedFilters)!;

            return new SavedSearchDto(
                Id: s.Id,
                Name: s.Name,
                RawQuery: s.RawQuery,
                ParsedFilters: filters,
                NotifyOnMatch: s.NotifyOnMatch,
                IsActive: s.IsActive,
                CreatedAt: s.CreatedAt);
        }).ToList();
    }
}