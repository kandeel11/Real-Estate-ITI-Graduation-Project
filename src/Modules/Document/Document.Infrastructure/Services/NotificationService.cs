using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Infrastructure.Hubs;
using Document.Infrastructure.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace Document.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(INotificationRepository repository, IHubContext<NotificationHub> hub)
    {
        _repository = repository;
        _hub = hub;
    }

    public async Task CreateInAppAsync(
        Guid userId, string title, string body, NotificationType type,
        string? relatedEntityType = null, Guid? relatedEntityId = null,
        CancellationToken ct = default)
    {
        var notification = new Domain.Entities.Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(notification, ct);

        // Push the notification live to the user's browser without requiring a page refresh.
        await _hub.Clients
            .Group($"notification-{userId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                type = notification.Type.ToString(),
                notification.Title,
                notification.Body,
                notification.RelatedEntityType,
                notification.RelatedEntityId,
                createdAt = DateTime.SpecifyKind(notification.CreatedAt, DateTimeKind.Utc)
            }, ct);
    }
}
