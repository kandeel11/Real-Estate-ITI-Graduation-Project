using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Keeps the broker performance counters in sync with what happens in the
/// Document module (leads/inquiries) — cross-module via Shared.Kernel events.
/// </summary>
public class BrokerDealClosedHandler : INotificationHandler<BrokerDealClosedEvent>
{
    private readonly IBrokerRepository _brokers;
    public BrokerDealClosedHandler(IBrokerRepository brokers) => _brokers = brokers;

    public async Task Handle(BrokerDealClosedEvent notification, CancellationToken cancellationToken)
    {
        var broker = await _brokers.GetByUserIdAsync(notification.BrokerUserId, cancellationToken);
        if (broker is null) return; // owner-handled lead — nothing to record

        broker.RecordClosedDeal();
        await _brokers.SaveChangesAsync(cancellationToken);
    }
}

public class BrokerInquiryAcceptedHandler : INotificationHandler<BrokerInquiryAcceptedEvent>
{
    private readonly IBrokerRepository _brokers;
    public BrokerInquiryAcceptedHandler(IBrokerRepository brokers) => _brokers = brokers;

    public async Task Handle(BrokerInquiryAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var broker = await _brokers.GetByUserIdAsync(notification.BrokerUserId, cancellationToken);
        if (broker is null) return;

        broker.RecordResponseTime(notification.ResponseTimeMinutes);
        await _brokers.SaveChangesAsync(cancellationToken);
    }
}
