using MediatR;

namespace Document.Application.Queries.Lead.GetBrokerLeadPipeline;

public record GetBrokerLeadPipelineQuery(List<Guid> BrokerUserIds) : IRequest<List<BrokerLeadPipelineDto>>;

public sealed record BrokerLeadPipelineDto(
    Guid BrokerUserId,
    PipelineKanbanDto Kanban,
    int TotalLeads,
    int ClosedWon
);

public sealed record PipelineKanbanDto(
    List<LeadCardDto> New,
    List<LeadCardDto> Contacted,
    List<LeadCardDto> Negotiating,
    List<LeadCardDto> ClosedWon,
    List<LeadCardDto> ClosedLost
);

public sealed record LeadCardDto(
    Guid LeadId,
    Guid InquiryId,
    Guid PropertyId,
    Guid TenantId,
    decimal? DealValue,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
