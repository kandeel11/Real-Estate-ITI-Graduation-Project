using MediatR;
using Microsoft.Extensions.Logging;
using Property.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

/// <summary>
/// Marks a property as Rented when a deal closes without a lease —
/// independent broker path or company broker "no management" path.
/// No payment schedule is created because there is no lease.
/// </summary>
public sealed class PropertyRentedDirectlyEventHandler
    : INotificationHandler<PropertyRentedDirectlyEvent>
{
    private readonly IPropertyRepository _repository;
    private readonly ILogger<PropertyRentedDirectlyEventHandler> _logger;

    public PropertyRentedDirectlyEventHandler(
        IPropertyRepository repository,
        ILogger<PropertyRentedDirectlyEventHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(PropertyRentedDirectlyEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "PropertyRentedDirectlyEvent | PropertyId={PropertyId} — marking as Rented (no-management path)",
            notification.PropertyId);

        try
        {
            var property = await _repository.GetByIdAsync(notification.PropertyId, ct);
            if (property is null)
            {
                _logger.LogWarning(
                    "Property {PropertyId} not found for no-management deal close.",
                    notification.PropertyId);
                return;
            }

            property.MarkAsRented();
            await _repository.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Property {PropertyId} marked as Rented (no-management deal).",
                notification.PropertyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to mark property {PropertyId} as Rented (no-management path).",
                notification.PropertyId);
        }
    }
}
