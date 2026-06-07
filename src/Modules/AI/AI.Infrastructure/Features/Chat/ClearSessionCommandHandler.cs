// AI.Infrastructure/Features/Chat/ClearSessionCommandHandler.cs

using AI.Application.Features.Chat.Commands;
using AI.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AI.Infrastructure.Features.Chat;

public sealed class ClearSessionCommandHandler
    : IRequestHandler<ClearSessionCommand>
{
    private readonly ISessionStore _sessions;
    private readonly ILogger<ClearSessionCommandHandler> _logger;

    public ClearSessionCommandHandler(
        ISessionStore sessions,
        ILogger<ClearSessionCommandHandler> logger)
    {
        _sessions = sessions;
        _logger = logger;
    }

    public async Task Handle(
        ClearSessionCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Clearing session history | SessionId={SessionId}",
            command.SessionId);

        await _sessions.DeleteAsync(command.SessionId, cancellationToken);

        _logger.LogInformation(
            "Session cleared | SessionId={SessionId}",
            command.SessionId);
    }
}