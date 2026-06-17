using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.MakeDecision;

public record MakeDecisionCommand(
    Guid TicketId,
    ResolutionMethod ResolutionMethod,
    Guid DecidedById) : IRequest;

public class MakeDecisionHandler(
    IMaintenanceTicketRepository ticketRepo,
    ITicketAiEvaluationRepository aiRepo,
    IVendorAssignmentRepository assignmentRepo,
    IAutoVendorAssignmentService autoAssign)
    : IRequestHandler<MakeDecisionCommand>
{
    public async Task Handle(MakeDecisionCommand cmd, CancellationToken ct)
    {
        var ticket = await ticketRepo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        ticket.MakeDecision(cmd.ResolutionMethod);

        if (cmd.ResolutionMethod == ResolutionMethod.PlatformVendor)
        {
            var evaluation = await aiRepo.GetByTicketIdAsync(cmd.TicketId, ct)
                ?? throw new InvalidOperationException(
                    "Cannot auto-assign a vendor: AI evaluation not found for this ticket.");

            var vendorId = await autoAssign.FindVendorForCategoryAsync(evaluation.Category, ct)
                ?? throw new InvalidOperationException(
                    $"No maintenance company is available for category '{evaluation.Category}'. " +
                    "Please choose the External Technician option instead.");

            var assignment = ticket.AssignVendor(vendorId, cmd.DecidedById);
            await assignmentRepo.AddAsync(assignment, ct);
            await autoAssign.IncrementActiveTicketsAsync(vendorId, ct);
        }

        await ticketRepo.SaveChangesAsync(ct);
    }
}
