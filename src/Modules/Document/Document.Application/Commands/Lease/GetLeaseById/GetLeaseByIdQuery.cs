using MediatR;

namespace Document.Application.Queries.Lease.GetLeaseById;

public record GetLeaseByIdQuery(Guid LeaseId) : IRequest<LeaseDto?>;

public record LeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    Guid OwnerId,
    decimal? RentAmount,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    string ExtractionStatus,
    decimal? ExtractionConfidence,
    bool IsActive,
    string ContractFileUrl,
    DateTime CreatedAt);