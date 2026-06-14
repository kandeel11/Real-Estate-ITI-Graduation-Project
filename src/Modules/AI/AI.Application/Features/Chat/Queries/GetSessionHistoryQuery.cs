// AI.Application/Features/Chat/Queries/GetSessionHistoryQuery.cs

using MediatR;

namespace AI.Application.Features.Chat.Queries;

public sealed record GetSessionHistoryQuery(
    Guid SessionId
) : IRequest<IReadOnlyList<SessionMessage>>;

public sealed record SessionMessage(
    string Role,
    string Content
);