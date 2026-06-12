namespace Payment.Application.Interfaces;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ProcessPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string PaymentMethod,
    string? CardToken = null,
    string? Description = null
);

public sealed record PaymentGatewayResult(
    bool IsSuccess,
    string? TransactionId,
    string? ReceiptUrl,
    string? ErrorMessage
);