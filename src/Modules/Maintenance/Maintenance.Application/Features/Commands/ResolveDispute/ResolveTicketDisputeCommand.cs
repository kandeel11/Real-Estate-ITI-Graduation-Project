using MediatR;

namespace Maintenance.Application.Features.Commands.ResolveDispute;

public record ResolveTicketDisputeCommand(
    Guid TicketId,
    /// <summary>"Owner" or "Tenant" — sets the final responsible party.</summary>
    string FinalResponsibleParty,
    string? AdminNotes) : IRequest;
