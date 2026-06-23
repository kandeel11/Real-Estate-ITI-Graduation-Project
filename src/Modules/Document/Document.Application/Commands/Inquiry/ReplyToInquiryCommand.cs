using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Inquiry;

/// <summary>
/// A broker/owner replies to an inquiry. Ensures a conversation exists, appends
/// the message, and notifies the tenant.
/// </summary>
public record ReplyToInquiryCommand(Guid InquiryId, Guid SenderId, string Text) : IRequest;

public class ReplyToInquiryCommandHandler : IRequestHandler<ReplyToInquiryCommand>
{
    private readonly IInquiryRepository _inquiries;
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public ReplyToInquiryCommandHandler(
        IInquiryRepository inquiries,
        IDocumentDbContext db,
        INotificationService notifications)
    {
        _inquiries = inquiries;
        _db = db;
        _notifications = notifications;
    }

    public async Task Handle(ReplyToInquiryCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Text))
            throw new InvalidOperationException("Reply text is required.");

        var inquiry = await _inquiries.GetByIdAsync(cmd.InquiryId, ct)
            ?? throw new KeyNotFoundException($"Inquiry {cmd.InquiryId} not found.");

        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.InquiryId == inquiry.Id, ct);

        if (conversation is null)
        {
            conversation = new Domain.Entities.Conversation
            {
                Id = Guid.NewGuid(),
                InquiryId = inquiry.Id,
                PropertyId = inquiry.PropertyId,
                TenantId = inquiry.TenantId,
                BrokerOrOwnerId = cmd.SenderId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
        }

        _db.Messages.Add(new Domain.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = cmd.SenderId,
            Content = cmd.Text.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        inquiry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notifications.CreateInAppAsync(
            userId: inquiry.TenantId,
            title: "New Reply to Your Inquiry",
            body: cmd.Text.Trim(),
            type: NotificationType.NewMessage,
            relatedEntityType: "inquiry",
            relatedEntityId: inquiry.Id,
            ct: ct);
    }
}
