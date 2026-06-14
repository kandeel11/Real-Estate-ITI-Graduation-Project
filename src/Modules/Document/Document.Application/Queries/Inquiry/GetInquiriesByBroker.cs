using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Inquiry;

/// <summary>
/// Inquiries assigned to a broker via InquiryAssignment.AssignedToId
/// (which holds the broker's identity UserId, matching the rest of the app).
/// </summary>
public record GetInquiriesByBrokerQuery(Guid BrokerUserId) : IRequest<List<InquiryResponse>>;

public class GetInquiriesByBrokerHandler : IRequestHandler<GetInquiriesByBrokerQuery, List<InquiryResponse>>
{
    private readonly IDocumentDbContext _db;

    public GetInquiriesByBrokerHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<InquiryResponse>> Handle(GetInquiriesByBrokerQuery query, CancellationToken ct)
    {
        var assignedInquiryIds = await _db.InquiryAssignments
            .Where(a => a.AssignedToId == query.BrokerUserId)
            .Select(a => a.InquiryId)
            .ToListAsync(ct);

        if (assignedInquiryIds.Count == 0)
            return [];

        var inquiries = await _db.Inquiries
            .Where(i => assignedInquiryIds.Contains(i.Id))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return inquiries.Select(i => CreateInquiryCommandHandler.MapToResponse(i)).ToList();
    }
}
