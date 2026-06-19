using MediatR;

namespace Payment.Application.Features.Commands.GenerateLeasePayment;

public record GenerateLeasePaymentCommand(Guid LeaseId) : IRequest<GenerateLeasePaymentResult>;

public record GenerateLeasePaymentResult(
    bool Generated,
    Guid? PaymentId,
    DateOnly DueDate,
    string Message
);
