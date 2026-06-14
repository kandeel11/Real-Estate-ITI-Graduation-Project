using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Queries.GetComments;

public record GetCommentsQuery(
    Guid TicketId, bool IncludeInternal = false,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<CommentDto>>;

public class GetCommentsHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<GetCommentsQuery, PagedResult<CommentDto>>
{
    public async Task<PagedResult<CommentDto>> Handle(GetCommentsQuery q, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(q.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {q.TicketId} not found.");

        var comments = ticket.Comments
            .Where(c => c.DeletedAt == null)
            .Where(c => q.IncludeInternal || !c.IsInternal)
            .OrderBy(c => c.CreatedAt).ToList();

        var total = comments.Count;
        var items = comments
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(c => new CommentDto(
                c.Id, c.UserId, c.UserRole.ToString(),
                c.Content, c.IsInternal, c.CreatedAt, c.UpdatedAt))
            .ToList();

        return new PagedResult<CommentDto>(items, total, q.Page, q.PageSize);
    }
}