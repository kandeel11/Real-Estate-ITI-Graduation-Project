using MediatR;
using Microsoft.Extensions.Logging;
using Property.Domain.Events;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Restores the property usage slot when a rented listing returns to the market —
/// the inverse of <see cref="PropertyRentedEventHandler"/>.
/// </summary>
public class PropertyBecameAvailableEventHandler : INotificationHandler<PropertyBecameAvailableEvent>
{
    private readonly ISubscriptionLimitChecker _limitChecker;
    private readonly ILogger<PropertyBecameAvailableEventHandler> _logger;

    public PropertyBecameAvailableEventHandler(
        ISubscriptionLimitChecker limitChecker,
        ILogger<PropertyBecameAvailableEventHandler> logger)
    {
        _limitChecker = limitChecker;
        _logger = logger;
    }

    public async Task Handle(PropertyBecameAvailableEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _limitChecker.IncrementPropertyUsageAsync(
                notification.OwnerId,
                notification.CompanyId,
                cancellationToken);

            _logger.LogInformation(
                "Restored property usage for OwnerId={OwnerId} CompanyId={CompanyId} after PropertyId={PropertyId} became available.",
                notification.OwnerId, notification.CompanyId, notification.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to restore property usage for PropertyId={PropertyId}.",
                notification.PropertyId);
        }
    }
}
