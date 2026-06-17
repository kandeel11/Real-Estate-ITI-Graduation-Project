using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Conversation.GetConversationsByUser;

public class GetConversationsByUserHandler 
    : IRequestHandler<GetConversationsByUserQuery, List<ConversationDto>>
{
    private readonly IDocumentDbContext _db;

    public GetConversationsByUserHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<ConversationDto>> Handle(
        GetConversationsByUserQuery query, CancellationToken ct)
    {
        return await _db.Conversations
            .Where(c => c.TenantId == query.UserId || c.BrokerOrOwnerId == query.UserId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationDto(
                c.Id, c.InquiryId, c.PropertyId,
                c.TenantId, c.BrokerOrOwnerId,
                c.IsActive, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);
    }
}