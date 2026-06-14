using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.MarkCompleted;

public record MarkCompletedCommand(Guid TicketId) : IRequest;

public class MarkCompletedHandler(IMaintenanceTicketRepository repo) : IRequestHandler<MarkCompletedCommand>
{
    public async Task Handle(MarkCompletedCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.ResolutionMethod != ResolutionMethod.ExternalTechnician)
            throw new InvalidOperationException(
                "Only tickets resolved via an external technician can be marked complete by the user.");

        ticket.CompleteWork();
        await repo.SaveChangesAsync(ct);
    }
}
