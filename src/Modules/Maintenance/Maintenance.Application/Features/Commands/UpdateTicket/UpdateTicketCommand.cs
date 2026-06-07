using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.UpdateTicket;

public record UpdateTicketCommand(Guid TicketId, string? Title, string? Description, string? Priority) : IRequest;

public class UpdateTicketHandler(IMaintenanceTicketRepository repo) : IRequestHandler<UpdateTicketCommand>
{
    public async Task Handle(UpdateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        TicketPriority? priority = cmd.Priority is not null
            && Enum.TryParse<TicketPriority>(cmd.Priority, true, out var p) ? p : null;
        ticket.Update(cmd.Title, cmd.Description, priority);
        await repo.SaveChangesAsync(ct);
    }
}