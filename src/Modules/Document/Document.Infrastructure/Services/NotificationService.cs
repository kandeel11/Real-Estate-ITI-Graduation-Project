using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Infrastructure.Repositories;
namespace Document.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    public NotificationService(INotificationRepository repository) => _repository = repository;

    public async Task CreateInAppAsync(
        Guid userId, string title, string body, NotificationType type,
        string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
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
    }
}