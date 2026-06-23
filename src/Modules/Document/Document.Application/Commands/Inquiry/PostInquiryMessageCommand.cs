using Document.Application.Interfaces;
using Document.Application.Queries.Message.GetMessagesByConversation;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Inquiry;

/// <summary>
/// Posts a chat message on an inquiry thread. Unlike <see cref="ReplyToInquiryCommand"/>
/// (broker/owner only), either party — the tenant OR the broker/owner — may post.
/// Ensures a conversation exists, appends the message, notifies the other side, and
/// returns the saved message.
/// </summary>
public record PostInquiryMessageCommand(Guid InquiryId, Guid SenderId, string Text)
    : IRequest<MessageDto>;

public class PostInquiryMessageCommandHandler
    : IRequestHandler<PostInquiryMessageCommand, MessageDto>
{
    private readonly IInquiryRepository _inquiries;
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public PostInquiryMessageCommandHandler(
        IInquiryRepository inquiries,
        IDocumentDbContext db,
        INotificationService notifications)
    {
        _inquiries = inquiries;
        _db = db;
        _notifications = notifications;
    }

    public async Task<MessageDto> Handle(PostInquiryMessageCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Text))
            throw new InvalidOperationException("Message text is required.");

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
                // If the broker/owner hasn't replied yet, fall back to the accepting
                // party (or owner) so the conversation has a counterpart.
                BrokerOrOwnerId = inquiry.AcceptedById ?? inquiry.OwnerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
        }

        var message = new Domain.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = cmd.SenderId,
            Content = cmd.Text.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(message);

        inquiry.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Notify the *other* participant.
        var senderIsTenant = cmd.SenderId == inquiry.TenantId;
        var recipientId = senderIsTenant ? conversation.BrokerOrOwnerId : inquiry.TenantId;
        if (recipientId != cmd.SenderId)
        {
            await _notifications.CreateInAppAsync(
                userId: recipientId,
                title: "New Message",
                body: cmd.Text.Trim(),
                type: NotificationType.NewMessage,
                relatedEntityType: "inquiry",
                relatedEntityId: inquiry.Id,
                ct: ct);
        }

        return new MessageDto(
            message.Id, message.ConversationId, message.SenderId,
            message.Content, message.IsRead, message.ReadAt, message.CreatedAt);
    }
}
