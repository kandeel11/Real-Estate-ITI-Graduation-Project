using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Message.GetInquiriesWithReply;

/// <summary>
/// For a set of inquiries, returns the IDs of those whose conversation contains
/// at least one message NOT sent by the tenant — i.e. the owner/broker has
/// replied. Lets the dashboard show "Replied" vs "Unanswered" accurately,
/// independent of the inquiry's lead lifecycle status.
/// </summary>
public record GetInquiriesWithReplyQuery(IReadOnlyCollection<Guid> InquiryIds)
    : IRequest<HashSet<Guid>>;

public class GetInquiriesWithReplyHandler
    : IRequestHandler<GetInquiriesWithReplyQuery, HashSet<Guid>>
{
    private readonly IDocumentDbContext _db;

    public GetInquiriesWithReplyHandler(IDocumentDbContext db) => _db = db;

    public async Task<HashSet<Guid>> Handle(GetInquiriesWithReplyQuery query, CancellationToken ct)
    {
        if (query.InquiryIds.Count == 0) return new HashSet<Guid>();

        var convs = await _db.Conversations
            .Where(c => c.InquiryId != null && query.InquiryIds.Contains(c.InquiryId.Value))
            .Select(c => new { c.Id, InquiryId = c.InquiryId!.Value, c.TenantId })
            .ToListAsync(ct);

        if (convs.Count == 0) return new HashSet<Guid>();

        var convIds = convs.Select(c => c.Id).ToList();
        var msgs = await _db.Messages
            .Where(m => convIds.Contains(m.ConversationId) && m.DeletedAt == null)
            .Select(m => new { m.ConversationId, m.SenderId })
            .ToListAsync(ct);

        var convById = convs.ToDictionary(c => c.Id);
        var replied = new HashSet<Guid>();
        foreach (var m in msgs)
        {
            var conv = convById[m.ConversationId];
            if (m.SenderId != conv.TenantId) replied.Add(conv.InquiryId);
        }
        return replied;
    }
}
