using Document.Application.Commands.Inquiry;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Document.Infrastructure.Hubs;

public class DocumentChatHub : Hub
{
    private readonly IMediator _mediator;

    public DocumentChatHub(IMediator mediator) => _mediator = mediator;

    public Task JoinConversation(Guid conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());

    /// <summary>
    /// Group key for inquiry-scoped chat. Both the hub and HTTP endpoints use this
    /// so clients only need to join one group per thread.
    /// </summary>
    public static string InquiryGroup(Guid inquiryId) => $"inquiry-{inquiryId}";

    public Task JoinInquiry(Guid inquiryId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, InquiryGroup(inquiryId));

    public Task LeaveInquiry(Guid inquiryId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, InquiryGroup(inquiryId));

    /// <summary>
    /// Sends a message on an inquiry thread. Persists the message to the database,
    /// triggers the in-app notification for the other party, and broadcasts
    /// "ReceiveMessage" to the inquiry group so all connected clients get it live.
    ///
    /// Frontend change required: pass <c>InquiryId</c> (not ConversationId).
    /// </summary>
    public async Task SendMessage(SendMessageDto dto)
    {
        // PostInquiryMessageCommand handles conversation creation (if needed),
        // DB persistence, and in-app notification for the other participant.
        var saved = await _mediator.Send(
            new PostInquiryMessageCommand(dto.InquiryId, dto.SenderId, dto.Content));

        await Clients.Group(InquiryGroup(dto.InquiryId))
            .SendAsync("ReceiveMessage", new
            {
                inquiryId = dto.InquiryId,
                message = new
                {
                    id = saved.Id,
                    conversationId = saved.ConversationId,
                    senderId = saved.SenderId,
                    text = saved.Content,
                    sentAt = DateTime.SpecifyKind(saved.CreatedAt, DateTimeKind.Utc),
                    isRead = false
                }
            });
    }
}

/// <param name="InquiryId">The inquiry this message belongs to (not the conversation id).</param>
public record SendMessageDto(
    Guid InquiryId,
    Guid SenderId,
    string Content);
