using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.StartWork;

public record StartWorkCommand(Guid TicketId) : IRequest;

public class StartWorkHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<StartWorkCommand>
{
    public async Task Handle(StartWorkCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        ticket.StartWork();
        await repo.SaveChangesAsync(ct);
    }
}