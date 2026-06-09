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
    private readonly IPublisher _publisher;
    private readonly ILogger<LeaseActivatedEventHandler> _logger;

    public LeaseActivatedEventHandler(
        IPropertyRepository repository,
        IPublisher publisher,
        ILogger<LeaseActivatedEventHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
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

            // ✅ Mark property as Rented
            // This validates Status=Active and publishes PropertyRentedEvent
            property.MarkAsRented();
            await _repository.SaveChangesAsync(cancellationToken);

            // Publish domain events (PropertyRentedEvent) to trigger subscription quota decrement
            foreach (var domainEvent in property.DomainEvents)
                await _publisher.Publish(domainEvent, cancellationToken);

            property.ClearDomainEvents();

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
