using MediatR;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised when an Admin resolves a disputed ticket by setting the final responsible party.
/// Consumers can notify the involved Tenant / Owner that the dispute has been settled.
/// </summary>
public sealed record TicketDisputeResolvedDomainEvent(
    Guid TicketId,
    string FinalResponsibleParty,
    string? AdminNotes) : INotification;
