using MediatR;
using Property.Application.Features.Commands.SetAiDuplicateScore;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertyDuplicateCheckCompletedHandler
    : INotificationHandler<PropertyDuplicateCheckCompletedEvent>
{
    private readonly IMediator _mediator;

    public PropertyDuplicateCheckCompletedHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(PropertyDuplicateCheckCompletedEvent notification, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new SetAiDuplicateScoreCommand(notification.PropertyId, notification.Confidence),
            cancellationToken);
    }
}
