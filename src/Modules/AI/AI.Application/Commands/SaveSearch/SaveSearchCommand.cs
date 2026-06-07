// AI.Application/Commands/SaveSearch/SaveSearchCommand.cs

using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Commands.SaveSearch;

public sealed record SaveSearchCommand(
    Guid UserId,
    Guid QueryId,       // from SmartSearchResult.QueryId
    string Name,
    bool NotifyOnMatch
) : IRequest<SavedSearchDto>;