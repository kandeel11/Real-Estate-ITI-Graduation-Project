using MediatR;

namespace Document.Application.Commands.Lease.ConfirmLease;

public record ConfirmLeaseCommand(
    Guid LeaseId,
    Guid OwnerId,
    decimal? RentAmount,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth) : IRequest;