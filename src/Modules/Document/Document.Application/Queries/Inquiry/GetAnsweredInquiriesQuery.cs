using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Inquiry;

/// <summary>
/// Of the given inquiries, returns the subset that have been "answered" — i.e. their
/// conversation holds at least one message from someone other than the tenant
/// (a broker/owner reply), regardless of whether it was sent via the inquiry tab
/// (<c>ReplyToInquiryCommand</c>) or the chat thread (<c>PostInquiryMessageCommand</c>).
/// </summary>
public record GetAnsweredInquiriesQuery(IReadOnlyCollection<Guid> InquiryIds)
    : IRequest<HashSet<Guid>>;

public class GetAnsweredInquiriesHandler(IDocumentDbContext db)
    : IRequestHandler<GetAnsweredInquiriesQuery, HashSet<Guid>>
{
    public async Task<HashSet<Guid>> Handle(GetAnsweredInquiriesQuery q, CancellationToken ct)
    {
        if (q.InquiryIds.Count == 0) return new HashSet<Guid>();

        var tenantByInquiry = await db.Inquiries
            .Where(i => q.InquiryIds.Contains(i.Id))
            .Select(i => new { i.Id, i.TenantId })
            .ToDictionaryAsync(x => x.Id, x => x.TenantId, ct);

        var convs = await db.Conversations
            .Where(c => c.InquiryId != null && q.InquiryIds.Contains(c.InquiryId.Value))
            .Select(c => new { c.Id, InquiryId = c.InquiryId!.Value })
            .ToListAsync(ct);

        if (convs.Count == 0) return new HashSet<Guid>();

        var convToInquiry = convs.ToDictionary(c => c.Id, c => c.InquiryId);
        var convIds = convs.Select(c => c.Id).ToList();

        var msgs = await db.Messages
            .Where(m => convIds.Contains(m.ConversationId) && m.DeletedAt == null)
            .Select(m => new { m.ConversationId, m.SenderId })
            .ToListAsync(ct);

        var answered = new HashSet<Guid>();
        foreach (var m in msgs)
        {
            if (!convToInquiry.TryGetValue(m.ConversationId, out var inquiryId)) continue;
            if (tenantByInquiry.TryGetValue(inquiryId, out var tenantId) && m.SenderId != tenantId)
                answered.Add(inquiryId);
        }

        return answered;
    }
}
