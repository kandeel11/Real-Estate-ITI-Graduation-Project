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
    IPropertyManagementChecker managementChecker)
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

            var result = await aiService.EvaluateAsync(
                ticket.TicketNumber!,
                ticket.LeaseId?.ToString() ?? string.Empty,
                ticket.Description, imageUrl, ct);

            var evaluation = TicketAiEvaluation.Create(
                ticket.Id, result.ResponsibleParty, result.Category,
                result.ConfidenceScore, result.Reasoning);
            await aiRepo.AddAsync(evaluation, ct);

            var responsible = result.ResponsibleParty == "Owner" || result.ResponsibleParty == "Landlord"
                 ? ResponsibleParty.Owner
                 : ResponsibleParty.Tenant;
            ticket.ApplyAiEvaluation(responsible);

            if (responsible == ResponsibleParty.Owner)
                await TryRouteToManagementCompanyAsync(ticket, ct);

            await repo.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AI Evaluation Failed] TicketId={ticket.Id}, Error={ex.Message}");
        }
    }

    private async Task TryRouteToManagementCompanyAsync(MaintenanceTicket ticket, CancellationToken ct)
    {
        try
        {
            var mgmt = await managementChecker.GetAsync(ticket.PropertyId, ct);
            if (mgmt is not { IsFullManagement: true, IsMaintenanceEnabled: true })
                return;

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
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Routing Failed] TicketId={ticket.Id}, Error={ex.Message}");
        }
    }
}