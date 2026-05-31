using Document.Domain.Enums;

namespace RealEstate.Shared.Kernel.Interfaces;

 
public interface INotificationService
{
    Task CreateInAppAsync(
        Guid userId,
        string title,
        string body,
        NotificationType type,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        CancellationToken ct = default);
}