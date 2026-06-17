using Document.Application.Commands.Message.SendMessage;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Document.Infrastructure.Hubs;

public class DocumentChatHub : Hub
{
    private readonly IMediator _mediator;

    public DocumentChatHub(IMediator mediator) => _mediator = mediator;

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task SendMessage(SendMessageDto dto)
    {
        var messageId = await _mediator.Send(new SendMessageCommand(
            dto.ConversationId,
            dto.SenderId,
            dto.Content));

        await Clients.Group(dto.ConversationId.ToString())
            .SendAsync("ReceiveMessage", new
            {
                Id = messageId,
                dto.ConversationId,
                dto.SenderId,
                dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
    }
}

public record SendMessageDto(
    Guid ConversationId,
    Guid SenderId,
    string Content);