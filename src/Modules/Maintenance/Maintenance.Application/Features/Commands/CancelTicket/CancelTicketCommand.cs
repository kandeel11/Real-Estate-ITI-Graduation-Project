using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.CancelTicket;

public record CancelTicketCommand(Guid TicketId, string CancelledBy) : IRequest;

public class CancelTicketHandler(IMaintenanceTicketRepository repo) : IRequestHandler<CancelTicketCommand>
{
    public async Task Handle(CancelTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        ticket.Cancel(cmd.CancelledBy);
        await repo.SaveChangesAsync(ct);
    }
}