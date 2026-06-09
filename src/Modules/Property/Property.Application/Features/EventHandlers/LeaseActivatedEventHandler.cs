using Document.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Ahmed subscribes to Yasser's LeaseActivatedEvent.
/// When a lease is activated, marks the associated property as Rented.
/// This triggers PropertyRentedEvent, which decrements subscription usage.
/// </summary>
public sealed class LeaseActivatedEventHandler : INotificationHandler<LeaseActivatedEvent>
{
    private readonly IPropertyRepository _repository;
    private readonly ILogger<LeaseActivatedEventHandler> _logger;

    public LeaseActivatedEventHandler(
        IPropertyRepository repository,
        ILogger<LeaseActivatedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(LeaseActivatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Property received LeaseActivatedEvent | LeaseId={LeaseId} PropertyId={PropertyId} — marking as rented",
            notification.LeaseId,
            notification.PropertyId);

        try
        {
            var property = await _repository.GetByIdAsync(notification.PropertyId, cancellationToken);
            if (property is null)
            {
                _logger.LogWarning(
                    "Property {PropertyId} not found for lease {LeaseId}",
                    notification.PropertyId,
                    notification.LeaseId);
                return;
            }

            // ✅ Mark property as Rented (validates Status=Active and raises PropertyRentedEvent).
            // PropertyRentedEvent is dispatched by DispatchDomainEventsInterceptor after save,
            // which triggers the subscription quota decrement.
            property.MarkAsRented();
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Property {PropertyId} marked as rented | Lease {LeaseId}",
                notification.PropertyId,
                notification.LeaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to mark property {PropertyId} as rented for lease {LeaseId}",
                notification.PropertyId,
                notification.LeaseId);
            // Non-critical — don't throw, lease activation should not fail if property marking fails
        }
    }
}
