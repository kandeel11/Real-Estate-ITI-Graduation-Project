// AI.Infrastructure/Handlers/DeleteSavedSearchHandler.cs

using AI.Application.Commands.DeleteSavedSearch;
using AI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Handlers;

public sealed class DeleteSavedSearchHandler
    : IRequestHandler<DeleteSavedSearchCommand>
{
    private readonly ISavedSearchRepository _repo;
    private readonly ILogger<DeleteSavedSearchHandler> _logger;

    public DeleteSavedSearchHandler(
        ISavedSearchRepository repo,
        ILogger<DeleteSavedSearchHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Handle(
        DeleteSavedSearchCommand command,
        CancellationToken ct)
    {
        var search = await _repo.GetByIdAsync(command.SavedSearchId, ct)
            ?? throw new KeyNotFoundException(
                $"Saved search '{command.SavedSearchId}' not found.");

        if (search.UserId != command.UserId)
            throw new UnauthorizedAccessException();

        search.Deactivate();
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Saved search {Id} deactivated by user {UserId}",
            command.SavedSearchId, command.UserId);
    }
}