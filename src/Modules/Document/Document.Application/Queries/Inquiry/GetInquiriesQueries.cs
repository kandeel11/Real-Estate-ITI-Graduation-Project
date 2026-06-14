using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Interfaces;
using MediatR;

namespace Document.Application.Queries.Inquiry;

public record GetInquiriesByPropertyQuery(Guid PropertyId) : IRequest<List<InquiryResponse>>;

public class GetInquiriesByPropertyHandler : IRequestHandler<GetInquiriesByPropertyQuery, List<InquiryResponse>>
{
    private readonly IInquiryRepository _inquiries;
    public GetInquiriesByPropertyHandler(IInquiryRepository inquiries) => _inquiries = inquiries;

    public async Task<List<InquiryResponse>> Handle(GetInquiriesByPropertyQuery query, CancellationToken ct)
    {
        var list = await _inquiries.GetByPropertyIdAsync(query.PropertyId, ct);
        return list.Select(i => CreateInquiryCommandHandler.MapToResponse(i)).ToList();
    }
}

public record GetMyInquiriesQuery(Guid TenantId) : IRequest<List<InquiryResponse>>;

public class GetMyInquiriesHandler : IRequestHandler<GetMyInquiriesQuery, List<InquiryResponse>>
{
    private readonly IInquiryRepository _inquiries;
    public GetMyInquiriesHandler(IInquiryRepository inquiries) => _inquiries = inquiries;

    public async Task<List<InquiryResponse>> Handle(GetMyInquiriesQuery query, CancellationToken ct)
    {
        var list = await _inquiries.GetByTenantIdAsync(query.TenantId, ct);
        return list.Select(i => CreateInquiryCommandHandler.MapToResponse(i)).ToList();
    }
}

public record GetInquiryByIdQuery(Guid InquiryId) : IRequest<InquiryResponse?>;

public class GetInquiryByIdHandler : IRequestHandler<GetInquiryByIdQuery, InquiryResponse?>
{
    private readonly IInquiryRepository _inquiries;
    public GetInquiryByIdHandler(IInquiryRepository inquiries) => _inquiries = inquiries;

    public async Task<InquiryResponse?> Handle(GetInquiryByIdQuery query, CancellationToken ct)
    {
        var inquiry = await _inquiries.GetByIdAsync(query.InquiryId, ct);
        return inquiry is null ? null : CreateInquiryCommandHandler.MapToResponse(inquiry);
    }
}