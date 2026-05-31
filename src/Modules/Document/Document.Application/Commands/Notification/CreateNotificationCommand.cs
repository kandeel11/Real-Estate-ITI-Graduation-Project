using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;

namespace Document.Application.Commands.Notification;

public record CreateNotificationCommand(
    Guid UserId,
    string Title,
    string Body,
    NotificationType Type,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null
) : IRequest<Guid>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Guid>
{
    private readonly INotificationRepository _notifications;

    public CreateNotificationCommandHandler(INotificationRepository notifications)
        => _notifications = notifications;

    public async Task<Guid> Handle(CreateNotificationCommand cmd, CancellationToken ct)
    {
        var notification = new Domain.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = cmd.UserId,
            Title = cmd.Title,
            Body = cmd.Body,
            Type = cmd.Type,
            RelatedEntityType = cmd.RelatedEntityType,
            RelatedEntityId = cmd.RelatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notifications.AddAsync(notification, ct);
        return notification.Id;
    }
}