using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published by the Document module when a lead assigned to a broker is
/// closed-won. Identity increments the broker's ClosedDealsCount.
/// BrokerUserId is the broker's USER id (InquiryAssignments.AssignedToId).
/// </summary>
public record BrokerDealClosedEvent(
    Guid BrokerUserId,
    Guid LeadId,
    decimal? DealValue) : INotification;

/// <summary>
/// Published by the Document module when a broker accepts an inquiry.
/// Identity folds the response time into the broker's AvgResponseTimeMinutes.
/// </summary>
public record BrokerInquiryAcceptedEvent(
    Guid BrokerUserId,
    int ResponseTimeMinutes) : INotification;
