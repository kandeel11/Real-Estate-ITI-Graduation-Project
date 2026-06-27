using MediatR;

namespace Document.Application.Queries.Lead.GetNoManagementPendingLeads;

public record GetNoManagementPendingLeadsQuery(Guid ManagerUserId)
    : IRequest<List<NoManagementPendingLeadDto>>;

public record NoManagementPendingLeadDto(
    Guid LeadId,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    decimal? DealValue,
    DateTime PendingAt
);
