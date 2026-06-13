using MediatR;

namespace Document.Application.Queries.Conversation.GetConversationsByUser;

public record GetConversationsByUserQuery(Guid UserId) : IRequest<List<ConversationDto>>;

public record ConversationDto(
    Guid Id,
    Guid? InquiryId,
    Guid PropertyId,
    Guid TenantId,
    Guid BrokerOrOwnerId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);