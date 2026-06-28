using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.ResolveDispute;

public class ResolveTicketDisputeHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<ResolveTicketDisputeCommand>
{
    public async Task Handle(ResolveTicketDisputeCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        ticket.ResolveDispute(cmd.FinalResponsibleParty, cmd.AdminNotes);

        await repo.SaveChangesAsync(ct);
    }
}
