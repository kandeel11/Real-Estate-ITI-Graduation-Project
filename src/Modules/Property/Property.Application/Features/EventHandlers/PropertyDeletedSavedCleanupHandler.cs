using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// When a property is deleted, drop every tenant's wishlist entry for it so the
/// rows don't linger as orphans counting toward anyone's saved-listings limit.
/// </summary>
public class PropertyDeletedSavedCleanupHandler : INotificationHandler<PropertyDeletedEvent>
{
    private readonly ISavedPropertyRepository _savedRepo;
    private readonly ILogger<PropertyDeletedSavedCleanupHandler> _logger;

    public PropertyDeletedSavedCleanupHandler(
        ISavedPropertyRepository savedRepo,
        ILogger<PropertyDeletedSavedCleanupHandler> logger)
    {
        _savedRepo = savedRepo;
        _logger = logger;
    }

    public async Task Handle(PropertyDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var removed = await _savedRepo.RemoveByPropertyAsync(notification.PropertyId, cancellationToken);
            if (removed > 0)
                _logger.LogInformation(
                    "Removed {Count} saved-property entries for deleted PropertyId={PropertyId}.",
                    removed, notification.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to clean up saved-property entries for deleted PropertyId={PropertyId}.",
                notification.PropertyId);
        }
    }
}
