using Document.Application.Queries.Message.GetMessagesByConversation;
using MediatR;

namespace Document.Application.Queries.Message.GetMessagesByInquiry;

/// <summary>
/// Returns the conversation thread for an inquiry (chat UI), keyed on InquiryId.
/// Mirrors <see cref="GetMessagesByConversationQuery"/> but resolves the conversation
/// from the inquiry. Returns an empty list when no conversation exists yet.
/// </summary>
public record GetMessagesByInquiryQuery(Guid InquiryId) : IRequest<List<MessageDto>>;
