using MediatR;
using Microsoft.AspNetCore.Http;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.UploadAttachment;

public record UploadAttachmentCommand(Guid TicketId, IFormFile File, Guid UploadedById) : IRequest<Guid>;

public class UploadAttachmentHandler(IMaintenanceTicketRepository repo, IFileStorageService storage)
    : IRequestHandler<UploadAttachmentCommand, Guid>
{
    private static readonly string[] Allowed =
        ["image/jpeg", "image/png", "image/webp", "video/mp4", "application/pdf"];

    public async Task<Guid> Handle(UploadAttachmentCommand cmd, CancellationToken ct)
    {
        if (!Allowed.Contains(cmd.File.ContentType))
            throw new ArgumentException($"File type {cmd.File.ContentType} is not allowed.");
        if (cmd.File.Length > 10 * 1024 * 1024)
            throw new ArgumentException("File must not exceed 10 MB.");

        var ticket = await repo.GetByIdWithDetailsAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        var terminal = new[] { TicketStatus.Completed, TicketStatus.Cancelled, TicketStatus.Rejected };
        if (terminal.Contains(ticket.Status))
            throw new InvalidOperationException("Cannot upload to a completed/cancelled/rejected ticket.");

        var url = await storage.UploadAsync(cmd.File, "maintenance", ct);
        var attachment = MaintenanceTicketAttachment.Create(
            cmd.TicketId, url, cmd.File.ContentType, (int)(cmd.File.Length / 1024), cmd.UploadedById);
        ticket.Attachments.Add(attachment);
        await repo.SaveChangesAsync(ct);
        return attachment.Id;
    }
}