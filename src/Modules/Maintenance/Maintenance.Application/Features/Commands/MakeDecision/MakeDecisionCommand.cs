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
            // Company-facing shadow tickets share the parent's AI evaluation
            var evalTicketId = ticket.ParentTicketId ?? cmd.TicketId;
            var evaluation = await aiRepo.GetByTicketIdAsync(evalTicketId, ct)
                ?? throw new InvalidOperationException(
                    "AI evaluation not found — cannot auto-assign a vendor.");

            var city = ExtractCity(ticket.PropertyAddress);
            var vendorId = await autoAssign.FindVendorForCategoryAsync(evaluation.Category, city, ct)
                ?? throw new InvalidOperationException(
                    $"No active maintenance company found for category '{evaluation.Category}'" +
                    (string.IsNullOrEmpty(city) ? "." : $" in '{city}'.") +
                    " Please choose External Technician instead.");

            var assignment = ticket.AssignVendor(vendorId, cmd.DecidedById);
            await assignmentRepo.AddAsync(assignment, ct);
            await autoAssign.IncrementActiveTicketsAsync(vendorId, ct);
        }

        await ticketRepo.SaveChangesAsync(ct);
    }

    private static string ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }
}
