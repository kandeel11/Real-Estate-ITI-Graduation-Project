using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Message.GetMessagesByConversation;

public class GetMessagesByConversationHandler 
    : IRequestHandler<GetMessagesByConversationQuery, List<MessageDto>>
{
    private readonly IDocumentDbContext _db;

    public GetMessagesByConversationHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<MessageDto>> Handle(
        GetMessagesByConversationQuery query, CancellationToken ct)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == query.ConversationId && m.DeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => new MessageDto(
                m.Id, m.ConversationId, m.SenderId,
                m.Content, m.IsRead, m.ReadAt, m.CreatedAt))
            .ToListAsync(ct);
    }
}