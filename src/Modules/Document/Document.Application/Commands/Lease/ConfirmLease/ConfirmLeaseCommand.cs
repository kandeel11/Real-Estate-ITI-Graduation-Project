using MediatR;

namespace Document.Application.Commands.Lease.ConfirmLease;

public record ConfirmLeaseCommand(
    Guid LeaseId,
    Guid ConfirmerId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth) : IRequest<ConfirmLeaseResult>;

public record ConfirmLeaseResult(
    bool IsActive,
    bool OwnerConfirmed,
    bool TenantConfirmed,
    string Status);

public record RejectLeaseCommand(
    Guid LeaseId,
    Guid TenantId,
    string Reason) : IRequest<RejectLeaseResult>;

public record RejectLeaseResult(bool Success, string Status);
