using MediatR;

namespace Document.Application.Queries.Lead.GetLeadsByBroker;

public record GetLeadsByBrokerQuery(Guid BrokerId) : IRequest<List<LeadDto>>;

public record LeadDto(
    Guid Id,
    Guid InquiryId,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    string Status,
    decimal? DealValue,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? NoManagementPendingAt = null,
    Guid? NoManagementManagerId = null);