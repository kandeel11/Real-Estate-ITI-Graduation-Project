// AI.Application/Queries/GetSavedSearches/GetSavedSearchesQuery.cs

using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Queries.GetSavedSearches;

public sealed record GetSavedSearchesQuery(
    Guid UserId
) : IRequest<IReadOnlyList<SavedSearchDto>>;