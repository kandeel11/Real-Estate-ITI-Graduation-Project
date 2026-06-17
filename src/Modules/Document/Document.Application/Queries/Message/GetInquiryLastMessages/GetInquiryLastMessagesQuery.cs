using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Message.GetInquiryLastMessages;

public record InquiryLastMessage(string Text, DateTime At);

/// <summary>
/// For a set of inquiries, returns the latest conversation message per inquiry
/// (text + timestamp). Used to show a preview and sort chats newest-first.
/// Inquiries with no conversation messages are simply absent from the map
/// (callers fall back to the original inquiry message).
/// </summary>
public record GetInquiryLastMessagesQuery(IReadOnlyCollection<Guid> InquiryIds)
    : IRequest<Dictionary<Guid, InquiryLastMessage>>;

public class GetInquiryLastMessagesHandler
    : IRequestHandler<GetInquiryLastMessagesQuery, Dictionary<Guid, InquiryLastMessage>>
{
    private readonly IDocumentDbContext _db;

    public GetInquiryLastMessagesHandler(IDocumentDbContext db) => _db = db;

    public async Task<Dictionary<Guid, InquiryLastMessage>> Handle(
        GetInquiryLastMessagesQuery query, CancellationToken ct)
    {
        if (query.InquiryIds.Count == 0)
            return new Dictionary<Guid, InquiryLastMessage>();

        var convs = await _db.Conversations
            .Where(c => c.InquiryId != null && query.InquiryIds.Contains(c.InquiryId.Value))
            .Select(c => new { c.Id, InquiryId = c.InquiryId!.Value })
            .ToListAsync(ct);

        if (convs.Count == 0)
            return new Dictionary<Guid, InquiryLastMessage>();

        var convToInquiry = convs.ToDictionary(c => c.Id, c => c.InquiryId);
        var convIds = convs.Select(c => c.Id).ToList();

        var msgs = await _db.Messages
            .Where(m => convIds.Contains(m.ConversationId) && m.DeletedAt == null)
            .Select(m => new { m.ConversationId, m.Content, m.CreatedAt })
            .ToListAsync(ct);

        return msgs
            .GroupBy(m => convToInquiry[m.ConversationId])
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var last = g.OrderByDescending(x => x.CreatedAt).First();
                    return new InquiryLastMessage(last.Content, last.CreatedAt);
                });
    }
}
