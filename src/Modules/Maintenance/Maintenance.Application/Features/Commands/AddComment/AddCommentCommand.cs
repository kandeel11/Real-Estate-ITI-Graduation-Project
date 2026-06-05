using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.AddComment;

public record AddCommentCommand(
    Guid TicketId, Guid UserId, CommentAuthorRole UserRole,
    string Content, bool IsInternal) : IRequest<Guid>;

public class AddCommentHandler(IMaintenanceTicketRepository repo) : IRequestHandler<AddCommentCommand, Guid>
{
    public async Task<Guid> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        var isInternal = cmd.UserRole == CommentAuthorRole.Tenant ? false : cmd.IsInternal;
        var comment = MaintenanceTicketComment.Create(cmd.TicketId, cmd.UserId, cmd.UserRole, cmd.Content, isInternal);
        ticket.Comments.Add(comment);
        await repo.SaveChangesAsync(ct);
        return comment.Id;
    }
}