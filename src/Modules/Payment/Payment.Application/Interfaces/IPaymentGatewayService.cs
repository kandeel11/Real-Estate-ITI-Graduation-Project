namespace Payment.Application.Interfaces;

/// <summary>
/// Contract للـ Payment Gateway
/// كل gateway (Stripe, PayMob, Fawry...) هيعمل implementation لهذه الـ interface
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// عملية الدفع الفعلية عبر الـ gateway
    /// </summary>
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// الطلب المرسل للـ gateway
/// </summary>
public sealed record ProcessPaymentRequest(
    Guid PaymentId,
    decimal Amount,
    string PaymentMethod,
    string? CardToken = null,
    string? Description = null
);

/// <summary>
/// نتيجة محاولة الدفع
/// </summary>
public sealed record PaymentGatewayResult(
    bool IsSuccess,
    string? TransactionId,
    string? ReceiptUrl,
    string? ErrorMessage
);
