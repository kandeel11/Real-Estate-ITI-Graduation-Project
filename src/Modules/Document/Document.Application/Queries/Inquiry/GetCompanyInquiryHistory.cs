using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Inquiry;

/// <summary>
/// Raw inquiry history for a company (ids only — the Host enriches tenant /
/// broker names and property titles, since those live in other modules).
/// AcceptedById is the broker user who handled the inquiry (null = pending).
/// </summary>
public record CompanyInquiryHistoryItemDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid? AcceptedById,
    string Status,
    string Message,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? ClosedAt);

public record GetCompanyInquiryHistoryQuery(Guid CompanyId)
    : IRequest<List<CompanyInquiryHistoryItemDto>>;

public class GetCompanyInquiryHistoryHandler
    : IRequestHandler<GetCompanyInquiryHistoryQuery, List<CompanyInquiryHistoryItemDto>>
{
    private readonly IInquiryStatsRepository _stats;
    public GetCompanyInquiryHistoryHandler(IInquiryStatsRepository stats) => _stats = stats;

    public async Task<List<CompanyInquiryHistoryItemDto>> Handle(
        GetCompanyInquiryHistoryQuery query, CancellationToken ct)
    {
        var inquiries = await _stats.GetAllByCompanyAsync(query.CompanyId, ct);

        return inquiries.Select(i => new CompanyInquiryHistoryItemDto(
            i.Id,
            i.PropertyId,
            i.TenantId,
            i.AcceptedById,
            i.Status.ToString(),
            i.Message,
            i.CreatedAt,
            i.AcceptedAt,
            i.ClosedAt)).ToList();
    }
}
