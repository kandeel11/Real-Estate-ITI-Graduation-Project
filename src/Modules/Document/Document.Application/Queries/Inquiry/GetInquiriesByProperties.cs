using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Inquiry;

/// <summary>
/// All inquiries raised on the given set of properties. Used by the broker
/// dashboard to show inquiries for every listing the broker manages.
/// </summary>
public record GetInquiriesByPropertiesQuery(IReadOnlyCollection<Guid> PropertyIds)
    : IRequest<List<InquiryResponse>>;

public class GetInquiriesByPropertiesHandler
    : IRequestHandler<GetInquiriesByPropertiesQuery, List<InquiryResponse>>
{
    private readonly IDocumentDbContext _db;

    public GetInquiriesByPropertiesHandler(IDocumentDbContext db) => _db = db;

    public async Task<List<InquiryResponse>> Handle(GetInquiriesByPropertiesQuery query, CancellationToken ct)
    {
        if (query.PropertyIds.Count == 0) return [];

        var ids = query.PropertyIds.ToList();
        var inquiries = await _db.Inquiries
            .Where(i => ids.Contains(i.PropertyId))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return inquiries.Select(i => CreateInquiryCommandHandler.MapToResponse(i)).ToList();
    }
}
