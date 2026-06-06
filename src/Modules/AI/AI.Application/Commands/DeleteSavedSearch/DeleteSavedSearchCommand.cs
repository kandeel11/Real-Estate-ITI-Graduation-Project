// AI.Application/Commands/DeleteSavedSearch/DeleteSavedSearchCommand.cs

using MediatR;

namespace AI.Application.Commands.DeleteSavedSearch;

public sealed record DeleteSavedSearchCommand(
    Guid SavedSearchId,
    Guid UserId          // for authorization
) : IRequest;