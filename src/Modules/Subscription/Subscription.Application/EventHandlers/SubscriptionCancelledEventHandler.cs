using MediatR;
using Microsoft.Extensions.Logging;
using Subscription.Domain.Events;

namespace Subscription.Application.EventHandlers;

public sealed class SubscriptionCancelledEventHandler
    : INotificationHandler<SubscriptionCancelledEvent>
{
    private readonly ILogger<SubscriptionCancelledEventHandler> _logger;

    public SubscriptionCancelledEventHandler(ILogger<SubscriptionCancelledEventHandler> logger)
        => _logger = logger;

    public Task Handle(SubscriptionCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Subscription cancelled | SubscriptionId: {SubscriptionId} | Reason: {Reason} | At: {CancelledAt}",
            notification.SubscriptionId,
            notification.Reason,
            notification.CancelledAt);

        return Task.CompletedTask;
    }
}
