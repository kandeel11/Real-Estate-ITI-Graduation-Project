using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.DeleteAttachment;

public record DeleteAttachmentCommand(Guid AttachmentId, Guid RequestingUserId) : IRequest;

public class DeleteAttachmentHandler(IMaintenanceTicketRepository repo)
    : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
    {
        var attachment = await repo.GetAttachmentByIdAsync(cmd.AttachmentId, ct)
            ?? throw new KeyNotFoundException("Attachment not found.");

        var ticket = await repo.GetByIdAsync(attachment.TicketId, ct)
            ?? throw new KeyNotFoundException("Ticket not found.");

        if (ticket.Status != TicketStatus.Draft)
            throw new InvalidOperationException("Attachments can only be removed while the ticket is Draft.");

        if (attachment.UploadedById != cmd.RequestingUserId)
            throw new UnauthorizedAccessException("You can only delete attachments you uploaded.");

        attachment.Remove();
        await repo.SaveChangesAsync(ct);
    }
}