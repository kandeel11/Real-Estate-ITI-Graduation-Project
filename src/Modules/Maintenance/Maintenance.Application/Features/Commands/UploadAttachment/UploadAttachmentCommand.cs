using MediatR;
using Microsoft.AspNetCore.Http;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.UploadAttachment;

public record UploadAttachmentCommand(Guid TicketId, IFormFile File, Guid UploadedById) : IRequest<Guid>;

public class UploadAttachmentHandler(
    IMaintenanceTicketRepository repo,
    IFileStorageService storage)
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

        // Load ticket WITHOUT includes — we only need status, no need to load collections
        var ticket = await repo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        var terminal = new[] { TicketStatus.Completed, TicketStatus.Cancelled, TicketStatus.Rejected };
        if (terminal.Contains(ticket.Status))
            throw new InvalidOperationException("Cannot upload to a completed/cancelled/rejected ticket.");

        // Upload file to Azure
        var url = await storage.UploadAsync(cmd.File, "maintenance", ct);
        var sizeKb = (int)(cmd.File.Length / 1024);

        var attachment = MaintenanceTicketAttachment.Create(
            cmd.TicketId, url, cmd.File.ContentType, sizeKb, cmd.UploadedById);

        // ← Add directly to DbSet instead of ticket.Attachments.Add()
        // This avoids EF Core marking the ticket as Modified and causing the concurrency error
        await repo.AddAttachmentAsync(attachment, ct);
        await repo.SaveChangesAsync(ct);

        return attachment.Id;
    }
}