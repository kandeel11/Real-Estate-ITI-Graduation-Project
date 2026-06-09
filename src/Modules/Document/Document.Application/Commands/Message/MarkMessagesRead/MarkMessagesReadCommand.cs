using MediatR;

namespace Document.Application.Commands.Message.MarkMessagesRead;

public record MarkMessagesReadCommand(
    Guid ConversationId,
    Guid ReaderId) : IRequest;