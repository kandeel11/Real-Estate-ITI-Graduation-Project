using MediatR;

namespace Maintenance.Application.Features.Commands.RaiseDispute;

public record RaiseTicketDisputeCommand(
    Guid TicketId,
    Guid DisputedById,
    string DisputedByRole,
    string Reason) : IRequest;
