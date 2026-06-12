using MediatR;

namespace Payment.Application.Features.Commands.ApplyLateFee;

public sealed record ApplyLateFeeCommand(
    Guid PaymentId,
    decimal LateFeeAmount
) : IRequest<bool>;
