using AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription;
using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace AI.Infrastructure.EventHandlers;

public sealed class PropertyAiDescriptionRequestedHandler
    : INotificationHandler<PropertyAiDescriptionRequestedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<PropertyAiDescriptionRequestedHandler> _logger;

    public PropertyAiDescriptionRequestedHandler(
        ISender sender,
        ILogger<PropertyAiDescriptionRequestedHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(PropertyAiDescriptionRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling PropertyAiDescriptionRequestedEvent for Property: {PropertyId}",
            notification.PropertyId);

        try
        {
            var command = new GeneratePropertyDescriptionCommand(
                PropertyId: notification.PropertyId,
                RequestedById: notification.OwnerId,
                PropertyType: notification.PropertyType,
                Location: $"{notification.District}, {notification.City}",
                Bedrooms: notification.Bedrooms,
                Area: notification.AreaSqm,
                Amenities: new List<string>()
            );

            var result = await _sender.Send(command, cancellationToken);

            _logger.LogInformation(
                "AI description generated for Property: {PropertyId}, GenerationId: {GenerationId}",
                notification.PropertyId, result.GenerationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate AI description for Property: {PropertyId}",
                notification.PropertyId);
        }
    }
}
