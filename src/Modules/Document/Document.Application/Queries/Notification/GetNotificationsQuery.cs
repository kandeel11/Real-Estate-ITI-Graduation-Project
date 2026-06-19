using Document.Application.DTOs;
using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Notification;

public record GetNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<NotificationListResult>;

public record NotificationListResult(List<NotificationResponse> Items, int Total, int UnreadCount);

public class GetNotificationsHandler(INotificationRepository repo)
    : IRequestHandler<GetNotificationsQuery, NotificationListResult>
{
    public async Task<NotificationListResult> Handle(GetNotificationsQuery q, CancellationToken ct)
    {
        var all = await repo.GetByUserIdAsync(q.UserId, unreadOnly: false, ct);
        var unread = all.Count(n => !n.IsRead);
        var filtered = q.UnreadOnly ? all.Where(n => !n.IsRead).ToList() : all;
        var total = filtered.Count;
        var items = filtered
            .OrderByDescending(n => n.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(n => new NotificationResponse(
                n.Id, n.UserId, n.Type.ToString(), n.Title, n.Body,
                n.RelatedEntityType, n.RelatedEntityId, n.IsRead,
                n.ReadAt.HasValue ? DateTime.SpecifyKind(n.ReadAt.Value, DateTimeKind.Utc) : null,
                DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)))
            .ToList();
        return new NotificationListResult(items, total, unread);
    }
}