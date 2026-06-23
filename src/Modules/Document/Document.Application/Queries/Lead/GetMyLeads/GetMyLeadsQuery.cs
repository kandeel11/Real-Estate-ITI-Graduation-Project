using MediatR;

namespace Document.Application.Queries.Lead.GetMyLeads;

public record GetMyLeadsQuery(Guid TenantId) : IRequest<List<TenantLeadDto>>;

/// <summary>
/// Lead pipeline view for the tenant. Includes the original inquiry message so the
/// tenant knows which property/request this lead corresponds to.
/// </summary>
public record TenantLeadDto(
    Guid LeadId,
    Guid InquiryId,
    Guid PropertyId,
    string PipelineStage,
    string InquiryMessage,
    string? Notes,
    decimal? DealValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt);
