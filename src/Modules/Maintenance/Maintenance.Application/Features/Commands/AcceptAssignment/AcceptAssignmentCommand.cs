using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.AcceptAssignment;

public record AcceptAssignmentCommand(Guid AssignmentId) : IRequest;

public class AcceptAssignmentHandler(IVendorAssignmentRepository assignmentRepo, IMaintenanceTicketRepository ticketRepo)
    : IRequestHandler<AcceptAssignmentCommand>
{
    public async Task Handle(AcceptAssignmentCommand cmd, CancellationToken ct)
    {
        var assignment = await assignmentRepo.GetByIdAsync(cmd.AssignmentId, ct)
            ?? throw new KeyNotFoundException($"Assignment {cmd.AssignmentId} not found.");
        assignment.Accept();
        var ticket = await ticketRepo.GetByIdAsync(assignment.TicketId, ct)
            ?? throw new KeyNotFoundException("Ticket not found.");
        ticket.VendorAccepted();
        await assignmentRepo.SaveChangesAsync(ct);
    }
}