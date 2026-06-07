using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Queries.GetTickets;

public record GetTicketsQuery(
    Guid? TenantId = null, Guid? OwnerId = null, Guid? PropertyId = null,
    TicketStatus? Status = null, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<TicketListItemDto>>;

public class GetTicketsHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<GetTicketsQuery, PagedResult<TicketListItemDto>>
{
    public async Task<PagedResult<TicketListItemDto>> Handle(GetTicketsQuery q, CancellationToken ct)
    {
        List<Maintenance.Domain.Entities.MaintenanceTicket> tickets;
        if (q.TenantId.HasValue)
            tickets = await repo.GetByTenantIdAsync(q.TenantId.Value, q.Status, ct);
        else if (q.OwnerId.HasValue)
            tickets = await repo.GetByOwnerIdAsync(q.OwnerId.Value, q.Status, ct);
        else if (q.PropertyId.HasValue)
            tickets = await repo.GetByPropertyIdAsync(q.PropertyId.Value, ct);
        else
            tickets = [];

        var total = tickets.Count;
        var items = tickets.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(t => new TicketListItemDto(t.Id, t.TicketNumber, t.Title,
                t.Priority.ToString(), t.Status.ToString(), t.PropertyAddress,
                t.Attachments.Count(a => a.DeletedAt == null), t.CreatedAt))
            .ToList();
        return new PagedResult<TicketListItemDto>(items, total, q.Page, q.PageSize);
    }
}