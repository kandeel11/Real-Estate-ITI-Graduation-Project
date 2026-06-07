using MediatR;
using Microsoft.Extensions.Logging;
using Property.Domain.Events;
using RealEstate.Shared.Kernel.Shared.Contracts;

namespace Property.Application.Features.EventHandlers;

public class PropertyRentedEventHandler : INotificationHandler<PropertyRentedEvent>
{
    private readonly ISubscriptionLimitChecker _limitChecker;
    private readonly ILogger<PropertyRentedEventHandler> _logger;

    public PropertyRentedEventHandler(ISubscriptionLimitChecker limitChecker, ILogger<PropertyRentedEventHandler> logger)
    {
        _limitChecker = limitChecker;
        _logger = logger;
    }

    public async Task Handle(PropertyRentedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _limitChecker.DecrementPropertyUsageAsync(
                notification.OwnerId,
                notification.CompanyId,
                cancellationToken);

            _logger.LogInformation(
                "Decremented property usage for OwnerId={OwnerId} CompanyId={CompanyId} after PropertyId={PropertyId} was rented.",
                notification.OwnerId, notification.CompanyId, notification.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to decrement property usage for PropertyId={PropertyId}.",
                notification.PropertyId);
        }
    }
}
