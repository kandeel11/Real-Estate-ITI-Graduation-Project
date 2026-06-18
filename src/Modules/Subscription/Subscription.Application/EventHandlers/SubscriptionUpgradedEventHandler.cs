using MediatR;
using Microsoft.Extensions.Logging;
using Subscription.Domain.Events;

namespace Subscription.Application.EventHandlers;

public sealed class SubscriptionUpgradedEventHandler
    : INotificationHandler<SubscriptionUpgradedEvent>
{
    private readonly ILogger<SubscriptionUpgradedEventHandler> _logger;

    public SubscriptionUpgradedEventHandler(ILogger<SubscriptionUpgradedEventHandler> logger)
        => _logger = logger;

    public Task Handle(SubscriptionUpgradedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Subscription upgraded | SubscriptionId: {SubscriptionId} | From: {FromPlanId} → To: {ToPlanId} | At: {UpgradedAt}",
            notification.SubscriptionId,
            notification.FromPlanId,
            notification.ToPlanId,
            notification.UpgradedAt);

        return Task.CompletedTask;
    }
}
