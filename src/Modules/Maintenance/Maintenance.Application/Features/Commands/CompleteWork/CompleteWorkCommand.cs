using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.CompleteWork;

public record CompleteWorkCommand(Guid TicketId) : IRequest;

public class CompleteWorkHandler(IMaintenanceTicketRepository repo) : IRequestHandler<CompleteWorkCommand>
{
    public async Task Handle(CompleteWorkCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        ticket.CompleteWork();
        await repo.SaveChangesAsync(ct);
    }
}