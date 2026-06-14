using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Queries.GetAdminTickets;

public record GetAdminTicketsQuery(
    TicketStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<AdminTicketsResult>;

public class GetAdminTicketsHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<GetAdminTicketsQuery, AdminTicketsResult>
{
    public async Task<AdminTicketsResult> Handle(GetAdminTicketsQuery q, CancellationToken ct)
    {
        var (tickets, total) = await repo.GetAllForAdminAsync(q.Status, q.Page, q.PageSize, ct);
        var statusCounts = await repo.GetStatusCountsAsync(ct);

        var items = tickets.Select(Map).ToList();
        var stats = BuildStats(statusCounts);

        return new AdminTicketsResult(items, total, q.Page, q.PageSize, stats);
    }

    private static AdminTicketDto Map(Maintenance.Domain.Entities.MaintenanceTicket t)
        => new(
            Id: t.Id,
            TicketNumber: t.TicketNumber,
            PropertyId: t.PropertyId,
            PropertyAddress: t.PropertyAddress,
            TenantId: t.TenantId,
            TenantName: null,           // enriched by controller
            TenantEmail: t.TenantEmail,
            TenantPhone: t.TenantPhone,
            OwnerId: t.OwnerId,
            OwnerName: null,            // enriched by controller
            Title: t.Title,
            Description: t.Description,
            Priority: t.Priority.ToString(),
            Status: t.Status.ToString(),
            ResponsibleParty: t.ResponsibleParty?.ToString(),
            ResolutionMethod: t.ResolutionMethod?.ToString(),
            AiEvaluation: t.AiEvaluation is null ? null : new AiEvaluationDto(
                t.AiEvaluation.Id,
                t.AiEvaluation.ResponsibleParty,
                t.AiEvaluation.Category,
                t.AiEvaluation.ConfidenceScore,
                t.AiEvaluation.Reasoning,
                t.AiEvaluation.EvaluatedAt),
            VendorAssignment: t.CurrentVendorAssignment is null ? null : new VendorAssignmentDto(
                t.CurrentVendorAssignment.Id,
                t.CurrentVendorAssignment.VendorId,
                t.CurrentVendorAssignment.Status.ToString(),
                t.CurrentVendorAssignment.RejectionReason,
                t.CurrentVendorAssignment.AcceptedAt,
                t.CurrentVendorAssignment.CreatedAt),
            VendorCompanyName: null,    // enriched by controller
            Attachments: t.Attachments
                .Where(a => a.DeletedAt == null)
                .Select(a => new AttachmentDto(a.Id, a.FileUrl, a.FileType, a.FileSizeKb, a.CreatedAt))
                .ToList(),
            Comments: t.Comments
                .Where(c => c.DeletedAt == null)
                .Select(c => new CommentDto(c.Id, c.UserId, c.UserRole.ToString(),
                    c.Content, c.IsInternal, c.CreatedAt, c.UpdatedAt))
                .ToList(),
            StatusHistory: t.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(h => new StatusHistoryItemDto(
                    h.FromStatus.ToString(), h.ToStatus.ToString(),
                    h.ChangedBy, h.Notes, h.CreatedAt))
                .ToList(),
            Payment: t.Payment is null ? null : new PaymentDto(
                t.Payment.Id, t.Payment.Amount, t.Payment.Currency,
                t.Payment.Status.ToString(), t.Payment.PaymentMethod,
                t.Payment.TransactionId, t.Payment.PaidAt, t.Payment.CreatedAt),
            CreatedAt: t.CreatedAt,
            UpdatedAt: t.UpdatedAt,
            ResolvedAt: t.ResolvedAt);

    private static TicketStatisticsDto BuildStats(Dictionary<TicketStatus, int> counts)
    {
        int Get(TicketStatus s) => counts.GetValueOrDefault(s, 0);
        return new TicketStatisticsDto(
            Total: counts.Values.Sum(),
            Draft: Get(TicketStatus.Draft),
            Submitted: Get(TicketStatus.Submitted),
            UnderAiReview: Get(TicketStatus.UnderAiReview),
            WaitingForDecision: Get(TicketStatus.WaitingForOwnerDecision)
                              + Get(TicketStatus.WaitingForTenantDecision)
                              + Get(TicketStatus.AiEvaluated),
            AssignedToVendor: Get(TicketStatus.AssignedToVendor)
                            + Get(TicketStatus.VendorAccepted),
            InProgress: Get(TicketStatus.InProgress),
            AwaitingPayment: Get(TicketStatus.AwaitingPayment),
            Completed: Get(TicketStatus.Completed),
            Cancelled: Get(TicketStatus.Cancelled),
            Rejected: Get(TicketStatus.Rejected));
    }
}
