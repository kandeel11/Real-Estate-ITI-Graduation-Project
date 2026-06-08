using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.RejectAssignment;

public record RejectAssignmentCommand(Guid AssignmentId, string Reason) : IRequest;

public class RejectAssignmentHandler(IVendorAssignmentRepository assignmentRepo, IMaintenanceTicketRepository ticketRepo)
    : IRequestHandler<RejectAssignmentCommand>
{
    public async Task Handle(RejectAssignmentCommand cmd, CancellationToken ct)
    {
        var assignment = await assignmentRepo.GetByIdAsync(cmd.AssignmentId, ct)
            ?? throw new KeyNotFoundException($"Assignment {cmd.AssignmentId} not found.");
        assignment.Reject(cmd.Reason);
        var ticket = await ticketRepo.GetByIdAsync(assignment.TicketId, ct)
            ?? throw new KeyNotFoundException("Ticket not found.");
        ticket.VendorRejected();
        await assignmentRepo.SaveChangesAsync(ct);
    }
}