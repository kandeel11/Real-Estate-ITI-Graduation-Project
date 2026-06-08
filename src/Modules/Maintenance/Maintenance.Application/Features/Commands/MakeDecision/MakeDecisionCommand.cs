using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.MakeDecision;

public record MakeDecisionCommand(Guid TicketId, ResolutionMethod ResolutionMethod) : IRequest;

public class MakeDecisionHandler(IMaintenanceTicketRepository repo) : IRequestHandler<MakeDecisionCommand>
{
    public async Task Handle(MakeDecisionCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        ticket.MakeDecision(cmd.ResolutionMethod);
        await repo.SaveChangesAsync(ct);
    }
}