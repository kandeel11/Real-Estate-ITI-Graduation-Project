using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.RaiseDispute;

public class RaiseTicketDisputeHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<RaiseTicketDisputeCommand>
{
    public async Task Handle(RaiseTicketDisputeCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        ticket.RaiseDispute(cmd.DisputedById, cmd.DisputedByRole, cmd.Reason);

        await repo.SaveChangesAsync(ct);
    }
}
