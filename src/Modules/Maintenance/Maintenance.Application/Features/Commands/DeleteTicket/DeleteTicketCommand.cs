using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.DeleteTicket;

public record DeleteTicketCommand(Guid TicketId) : IRequest;

public class DeleteTicketHandler(IMaintenanceTicketRepository repo) : IRequestHandler<DeleteTicketCommand>
{
    public async Task Handle(DeleteTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        ticket.SoftDelete();
        await repo.SaveChangesAsync(ct);
    }
}