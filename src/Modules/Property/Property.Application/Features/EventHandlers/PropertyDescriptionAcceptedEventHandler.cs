using MediatR;
using Property.Application.Features.Commands.SetAiDescription;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Property.Application.Features.EventHandlers;

public class PropertyDescriptionAcceptedEventHandler
    : INotificationHandler<PropertyDescriptionAcceptedEvent>
{
    private readonly IMediator _mediator;

    public PropertyDescriptionAcceptedEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(
        PropertyDescriptionAcceptedEvent notification,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetAiDescriptionCommand(
            notification.PropertyId,
            notification.DescriptionEn,
            notification.DescriptionAr
        ), cancellationToken);
    }
}
