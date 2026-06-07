using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.CreateTicket;

public record CreateTicketCommand(
    Guid TenantId, Guid PropertyId, Guid OwnerId, Guid? LeaseId,
    string Title, string Description, string Priority) : IRequest<Guid>;

public class CreateTicketHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<CreateTicketCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<TicketPriority>(cmd.Priority, true, out var priority))
            throw new ArgumentException($"Invalid priority: {cmd.Priority}");

        var ticket = MaintenanceTicket.Create(
            cmd.PropertyId, cmd.TenantId, cmd.OwnerId,
            cmd.Title, cmd.Description, priority, cmd.LeaseId);

        await repo.AddAsync(ticket, ct);
        await repo.SaveChangesAsync(ct);
        return ticket.Id;
    }
}