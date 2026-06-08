using MediatR;

namespace Document.Application.Queries.Message.GetMessagesByConversation;

public record GetMessagesByConversationQuery(
    Guid ConversationId,
    int Page = 1,
    int PageSize = 50) : IRequest<List<MessageDto>>;

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);