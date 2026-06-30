using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.SubmitTicket;

public record SubmitTicketCommand(
    Guid TicketId,
    string TenantEmail,     
    string TenantPhone,
    string PropertyAddress) : IRequest;

public class SubmitTicketHandler(
    IMaintenanceTicketRepository repo,
    ITicketNumberGenerator numberGen,
    IAiEvaluationService aiService,
    ITicketAiEvaluationRepository aiRepo,
    IPropertyManagementChecker managementChecker,
    IAutoVendorAssignmentService autoAssign,
    IVendorAssignmentRepository assignmentRepo)
    : IRequestHandler<SubmitTicketCommand>
{
    public async Task Handle(SubmitTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        var number = await numberGen.GenerateAsync(ct);
        ticket.Submit(number, cmd.PropertyAddress, cmd.TenantEmail, cmd.TenantPhone);
        await repo.SaveChangesAsync(ct);

        await TriggerAiAsync(ticket, ct);
    }

    private async Task TriggerAiAsync(MaintenanceTicket ticket, CancellationToken ct)
    {
        try
        {
            var imageUrl = ticket.Attachments
                .FirstOrDefault(a => a.FileType.StartsWith("image/") && a.DeletedAt == null)
                ?.FileUrl ?? string.Empty;

            // Fetch exact category names from DB so the AI returns a precise match
            var categoryNames = await autoAssign.GetCategoryNamesAsync(ct);

            var result = await aiService.EvaluateAsync(
                ticket.TicketNumber!,
                ticket.LeaseId?.ToString() ?? string.Empty,
                ticket.Description, imageUrl, categoryNames, ct);

            var evaluation = TicketAiEvaluation.Create(
                ticket.Id, result.ResponsibleParty, result.Category,
                result.ConfidenceScore, result.Reasoning);
            await aiRepo.AddAsync(evaluation, ct);

            var responsible = result.ResponsibleParty == "Owner" || result.ResponsibleParty == "Landlord"
                 ? ResponsibleParty.Owner
                 : ResponsibleParty.Tenant;
            ticket.ApplyAiEvaluation(responsible);

            if (responsible == ResponsibleParty.Owner)
            {
                // Full-management property → create company-facing ticket for the company manager
                var routed = await TryRouteToManagementCompanyAsync(ticket, ct);

                // Non-managed property → auto-assign the best matching maintenance company
                if (!routed)
                    await TryAutoAssignVendorAsync(ticket, result.Category, ct);
            }

            await repo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AI Evaluation Failed] TicketId={ticket.Id}, Error={ex.Message}");
        }
    }

    // Returns true if the ticket was routed to a management company.
    private async Task<bool> TryRouteToManagementCompanyAsync(MaintenanceTicket ticket, CancellationToken ct)
    {
        try
        {
            var mgmt = await managementChecker.GetAsync(ticket.PropertyId, ct);
            if (mgmt is not { IsFullManagement: true, IsMaintenanceEnabled: true })
                return false;

            var companyTicket = MaintenanceTicket.CreateForCompany(
                ticket.PropertyId,
                ticket.TenantId,
                mgmt.CompanyManagerUserId,
                ticket.Title,
                ticket.Description,
                ticket.Priority,
                ticket.Id,
                mgmt.CompanyId,
                ticket.LeaseId);

            await repo.AddAsync(companyTicket, ct);
            ticket.RouteToManagementCompany(mgmt.CompanyId, companyTicket.Id);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Routing Failed] TicketId={ticket.Id}, Error={ex.Message}");
            return false;
        }
    }

    // Finds a maintenance company by AI category + property city and assigns directly.
    // If no company matches, ticket stays at WaitingForOwnerDecision for manual resolution.
    private async Task TryAutoAssignVendorAsync(
        MaintenanceTicket ticket, string aiCategory, CancellationToken ct)
    {
        try
        {
            var city = ExtractCity(ticket.PropertyAddress);
            var vendorId = await autoAssign.FindVendorForCategoryAsync(aiCategory, city, ct);
            if (vendorId is null) return;

            ticket.MakeDecision(Domain.Enums.ResolutionMethod.PlatformVendor);
            var assignment = ticket.AssignVendor(vendorId.Value, Guid.Empty);
            await assignmentRepo.AddAsync(assignment, ct);
            await autoAssign.IncrementActiveTicketsAsync(vendorId.Value, ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Auto-Assign Failed] TicketId={ticket.Id}, Error={ex.Message}");
        }
    }

    private static string ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }
}