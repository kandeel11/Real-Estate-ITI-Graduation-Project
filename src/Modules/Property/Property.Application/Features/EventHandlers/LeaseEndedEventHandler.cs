using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// When a lease ends (early termination or expiry sweep), return the property to
/// Active. MarkAsAvailable raises PropertyBecameAvailableEvent, which restores the
/// subscription usage slot — the inverse of the rent flow.
/// </summary>
public class LeaseEndedEventHandler : INotificationHandler<LeaseEndedEvent>
{
    private readonly IPropertyRepository _repository;
    private readonly ILogger<LeaseEndedEventHandler> _logger;

    public LeaseEndedEventHandler(IPropertyRepository repository, ILogger<LeaseEndedEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(LeaseEndedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _repository.GetByIdAsync(notification.PropertyId, cancellationToken);
            if (property is null)
            {
                _logger.LogWarning(
                    "Property {PropertyId} not found for ended lease {LeaseId}.",
                    notification.PropertyId, notification.LeaseId);
                return;
            }

            property.MarkAsAvailable(); // no-op unless currently Rented
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Property {PropertyId} returned to market after lease {LeaseId} ended.",
                notification.PropertyId, notification.LeaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to return property {PropertyId} to market for ended lease {LeaseId}.",
                notification.PropertyId, notification.LeaseId);
            // Non-critical — don't throw.
        }
    }
}
