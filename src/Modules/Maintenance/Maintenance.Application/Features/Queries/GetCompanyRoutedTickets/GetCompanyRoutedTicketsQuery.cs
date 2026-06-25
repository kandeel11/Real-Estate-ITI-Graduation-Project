using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Queries.GetCompanyRoutedTickets;

public record GetCompanyRoutedTicketsQuery(Guid CompanyId) : IRequest<List<TicketDetailDto>>;

public class GetCompanyRoutedTicketsHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<GetCompanyRoutedTicketsQuery, List<TicketDetailDto>>
{
    public async Task<List<TicketDetailDto>> Handle(GetCompanyRoutedTicketsQuery q, CancellationToken ct)
    {
        var tickets = await repo.GetByRoutedCompanyIdAsync(q.CompanyId, ct);
        return tickets.Select(Map).ToList();
    }

    private static TicketDetailDto Map(Maintenance.Domain.Entities.MaintenanceTicket t)
        => new(
            t.Id,
            t.TicketNumber,
            t.PropertyId,
            t.PropertyAddress,
            t.TenantId,
            t.TenantEmail,
            t.TenantPhone,
            t.OwnerId,
            t.Title,
            t.Description,
            t.Priority.ToString(),
            t.Status.ToString(),
            t.ResponsibleParty?.ToString(),
            t.ResolutionMethod?.ToString(),
            t.AiEvaluation is null ? null : new AiEvaluationDto(
                t.AiEvaluation.Id,
                t.AiEvaluation.ResponsibleParty,
                t.AiEvaluation.Category,
                t.AiEvaluation.ConfidenceScore,
                t.AiEvaluation.Reasoning,
                t.AiEvaluation.EvaluatedAt),
            t.CurrentVendorAssignment is null ? null : new VendorAssignmentDto(
                t.CurrentVendorAssignment.Id,
                t.CurrentVendorAssignment.VendorId,
                t.CurrentVendorAssignment.Status.ToString(),
                t.CurrentVendorAssignment.RejectionReason,
                t.CurrentVendorAssignment.AcceptedAt,
                t.CurrentVendorAssignment.CreatedAt),
            t.Attachments.Where(a => a.DeletedAt == null)
                .Select(a => new AttachmentDto(a.Id, a.FileUrl, a.FileType, a.FileSizeKb, a.CreatedAt))
                .ToList(),
            t.Comments.Where(c => c.DeletedAt == null)
                .Select(c => new CommentDto(c.Id, c.UserId, c.UserRole.ToString(),
                    c.Content, c.IsInternal, c.CreatedAt, c.UpdatedAt))
                .ToList(),
            t.StatusHistory.OrderBy(h => h.CreatedAt)
                .Select(h => new StatusHistoryItemDto(
                    h.FromStatus.ToString(), h.ToStatus.ToString(),
                    h.ChangedBy, h.Notes, h.CreatedAt))
                .ToList(),
            t.Payment is null ? null : new PaymentDto(
                t.Payment.Id, t.Payment.Amount, t.Payment.Currency,
                t.Payment.Status.ToString(), t.Payment.PaymentMethod,
                t.Payment.TransactionId, t.Payment.PaidAt, t.Payment.CreatedAt,
                t.Payment.CheckoutUrl, t.Payment.CheckoutSessionId),
            t.CreatedAt,
            t.UpdatedAt,
            t.ResolvedAt,
            t.RoutedToCompanyId,
            t.ParentTicketId);
}
