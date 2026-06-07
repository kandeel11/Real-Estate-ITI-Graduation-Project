using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.AssignVendor;

public record AssignVendorCommand(Guid TicketId, Guid VendorId, Guid AssignedById) : IRequest<Guid>;

public class AssignVendorHandler(IMaintenanceTicketRepository ticketRepo, IVendorAssignmentRepository assignmentRepo)
    : IRequestHandler<AssignVendorCommand, Guid>
{
    public async Task<Guid> Handle(AssignVendorCommand cmd, CancellationToken ct)
    {
        var ticket = await ticketRepo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        var assignment = ticket.AssignVendor(cmd.VendorId, cmd.AssignedById);
        await assignmentRepo.AddAsync(assignment, ct);
        await ticketRepo.SaveChangesAsync(ct);
        return assignment.Id;
    }
}