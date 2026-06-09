using MediatR;

namespace Document.Application.Commands.Message.SendMessage;

public record SendMessageCommand(
    Guid ConversationId,
    Guid SenderId,
    string Content) : IRequest<Guid>;