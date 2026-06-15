using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lead.GetLeadByInquiry;

/// <summary>Returns the id of the lead created from an inquiry, or null if none exists.</summary>
public record GetLeadByInquiryQuery(Guid InquiryId) : IRequest<Guid?>;

public class GetLeadByInquiryHandler : IRequestHandler<GetLeadByInquiryQuery, Guid?>
{
    private readonly IDocumentDbContext _db;

    public GetLeadByInquiryHandler(IDocumentDbContext db) => _db = db;

    public async Task<Guid?> Handle(GetLeadByInquiryQuery query, CancellationToken ct)
    {
        return await _db.Leads
            .Where(l => l.InquiryId == query.InquiryId)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct);
    }
}
