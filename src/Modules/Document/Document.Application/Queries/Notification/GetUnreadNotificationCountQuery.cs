using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Notification;

public record GetUnreadNotificationCountQuery(Guid UserId) : IRequest<int>;

public class GetUnreadNotificationCountHandler(INotificationRepository repo)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public Task<int> Handle(GetUnreadNotificationCountQuery q, CancellationToken ct)
        => repo.GetUnreadCountAsync(q.UserId, ct);
}