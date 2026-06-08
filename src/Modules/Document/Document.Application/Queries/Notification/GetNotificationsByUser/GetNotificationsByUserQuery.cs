using MediatR;

namespace Document.Application.Queries.Notification.GetNotificationsByUser;

public record GetNotificationsByUserQuery(
    Guid UserId,
    bool OnlyUnread = false) : IRequest<List<NotificationDto>>;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime? ReadAt,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    DateTime CreatedAt);