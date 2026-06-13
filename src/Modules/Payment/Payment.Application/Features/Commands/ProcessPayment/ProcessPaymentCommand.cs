using MediatR;

namespace Payment.Application.Features.Commands.ProcessPayment;

public sealed record ProcessPaymentCommand(
    Guid PaymentId,
    string PaymentMethod,
    string? CardToken = null
) : IRequest<ProcessPaymentResult>;

public sealed record ProcessPaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? ReceiptUrl,
    string? ErrorMessage
);
