using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Queries.GetTicketStatistics;

public record GetTicketStatisticsQuery(Guid TenantId) : IRequest<TicketStatisticsDto>;

public class GetTicketStatisticsHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<GetTicketStatisticsQuery, TicketStatisticsDto>
{
    public async Task<TicketStatisticsDto> Handle(GetTicketStatisticsQuery q, CancellationToken ct)
    {
        var tickets = await repo.GetByTenantIdAsync(q.TenantId, status: null, ct);
        return new TicketStatisticsDto(
            Total: tickets.Count,
            Draft: tickets.Count(t => t.Status == TicketStatus.Draft),
            Submitted: tickets.Count(t => t.Status == TicketStatus.Submitted),
            UnderAiReview: tickets.Count(t => t.Status == TicketStatus.UnderAiReview),
            WaitingForDecision: tickets.Count(t =>
                t.Status == TicketStatus.WaitingForTenantDecision ||
                t.Status == TicketStatus.AiEvaluated),
            AssignedToVendor: tickets.Count(t =>
                t.Status == TicketStatus.AssignedToVendor ||
                t.Status == TicketStatus.VendorAccepted),
            InProgress: tickets.Count(t => t.Status == TicketStatus.InProgress),
            AwaitingPayment: tickets.Count(t => t.Status == TicketStatus.AwaitingPayment),
            Completed: tickets.Count(t => t.Status == TicketStatus.Completed),
            Cancelled: tickets.Count(t => t.Status == TicketStatus.Cancelled),
            Rejected: tickets.Count(t => t.Status == TicketStatus.Rejected));
    }
}