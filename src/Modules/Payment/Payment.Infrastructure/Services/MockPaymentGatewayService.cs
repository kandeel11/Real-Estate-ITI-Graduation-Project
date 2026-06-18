using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Mock implementation للـ Payment Gateway
/// ده للتطوير والاختبار — في الـ production هيتبدل بـ PayMob أو Stripe
/// 
/// نفس نمط الـ NullMediaStorageService في Property module
/// </summary>
public sealed class MockPaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<MockPaymentGatewayService> _logger;

    public MockPaymentGatewayService(ILogger<MockPaymentGatewayService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentGatewayResult> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[MockGateway] Processing payment {PaymentId} Amount={Amount} Method={Method}",
            request.PaymentId, request.Amount, request.PaymentMethod);

        // في الـ mock: دايماً هينجح ويرجع transaction ID وهمي
        var transactionId = $"TXN-{Guid.NewGuid():N}".ToUpper()[..20];
        var receiptUrl = $"https://receipts.example.com/{transactionId}";

        var result = new PaymentGatewayResult(
            IsSuccess: true,
            TransactionId: transactionId,
            ReceiptUrl: receiptUrl,
            ErrorMessage: null
        );

        return Task.FromResult(result);
    }
}
