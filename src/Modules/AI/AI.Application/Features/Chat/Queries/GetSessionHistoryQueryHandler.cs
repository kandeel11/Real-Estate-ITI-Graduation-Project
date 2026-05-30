// AI.Application/Features/Chat/Queries/GetSessionHistoryQueryHandler.cs

using AI.Application.Interfaces;
using MediatR;

namespace AI.Application.Features.Chat.Queries;

public sealed class GetSessionHistoryQueryHandler
    : IRequestHandler<GetSessionHistoryQuery, IReadOnlyList<SessionMessage>>
{
    private readonly ISessionStore _sessions;

    public GetSessionHistoryQueryHandler(ISessionStore sessions)
        => _sessions = sessions;

    public async Task<IReadOnlyList<SessionMessage>> Handle(
        GetSessionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var history = await _sessions.GetOrCreateAsync(
            query.SessionId, string.Empty, cancellationToken);

        return history
            .Where(m => m.Role.Label != "system")   // hide system prompt
            .Select(m => new SessionMessage(
                Role: m.Role.Label,
                Content: m.Content?.FirstOrDefault().ToString() ?? string.Empty))
            .ToList();
    }
}