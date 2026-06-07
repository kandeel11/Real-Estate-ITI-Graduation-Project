using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Notification.GetNotificationsByUser;

public class GetNotificationsByUserHandler 
    : IRequestHandler<GetNotificationsByUserQuery, List<NotificationDto>>
{
    private readonly IDocumentDbContext _db;

    public GetNotificationsByUserHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<NotificationDto>> Handle(
        GetNotificationsByUserQuery query, CancellationToken ct)
    {
        var notificationsQuery = _db.Notifications
            .Where(n => n.UserId == query.UserId);

        if (query.OnlyUnread)
            notificationsQuery = notificationsQuery.Where(n => !n.IsRead);

        return await notificationsQuery
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
            n.Id, n.Type.ToString(), n.Title, n.Body,
            n.IsRead, n.ReadAt,
            n.RelatedEntityType, n.RelatedEntityId,
            n.CreatedAt))
            .ToListAsync(ct);
    }
}