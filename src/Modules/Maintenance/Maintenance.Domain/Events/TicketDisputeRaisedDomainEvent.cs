using MediatR;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised when a Tenant, Owner, or Company Manager disputes the AI's responsibility
/// decision on a maintenance ticket. Consumers can use this to notify the admin team.
/// </summary>
public sealed record TicketDisputeRaisedDomainEvent(
    Guid TicketId,
    Guid DisputedById,
    string DisputedByRole,
    string Reason) : INotification;
