using Document.Application.Interfaces;
using Document.Application.Queries.Message.GetMessagesByConversation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Message.GetMessagesByInquiry;

public class GetMessagesByInquiryHandler
    : IRequestHandler<GetMessagesByInquiryQuery, List<MessageDto>>
{
    private readonly IDocumentDbContext _db;

    public GetMessagesByInquiryHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<MessageDto>> Handle(
        GetMessagesByInquiryQuery query, CancellationToken ct)
    {
        var conversationId = await _db.Conversations
            .Where(c => c.InquiryId == query.InquiryId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

        if (conversationId is null)
            return new List<MessageDto>();

        return await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id, m.ConversationId, m.SenderId,
                m.Content, m.IsRead, m.ReadAt, m.CreatedAt))
            .ToListAsync(ct);
    }
}
