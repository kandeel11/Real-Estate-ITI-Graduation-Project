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
        // Gather messages across ALL conversations for this inquiry, not just the
        // first. Historically a duplicate conversation could be created on accept,
        // splitting replies across rows; aggregating keeps every message visible.
        var conversationIds = await _db.Conversations
            .Where(c => c.InquiryId == query.InquiryId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (conversationIds.Count == 0)
            return new List<MessageDto>();

        return await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationId) && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id, m.ConversationId, m.SenderId,
                m.Content, m.IsRead, m.ReadAt, m.CreatedAt))
            .ToListAsync(ct);
    }
}
