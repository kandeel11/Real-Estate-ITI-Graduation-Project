namespace Subscription.Application.Interfaces;

public record PaymentResult(bool IsSuccess, string? TransactionId, string? Error);

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string customerToken, CancellationToken ct = default);
    Task<PaymentResult> RefundAsync(string transactionId, decimal amount, CancellationToken ct = default);
}
