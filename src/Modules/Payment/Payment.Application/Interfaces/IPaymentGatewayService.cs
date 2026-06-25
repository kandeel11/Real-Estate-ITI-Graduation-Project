namespace Payment.Application.Interfaces;

/// <summary>
/// Contract للـ Payment Gateway
/// كل gateway (Stripe, PayMob, Fawry...) هيعمل implementation لهذه الـ interface
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Server-side charge (requires card token already collected by frontend).
    /// </summary>
    Task<PaymentGatewayResult> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a PaymentIntent without confirming it — returns clientSecret so the
    /// frontend can render Stripe Elements and let the tenant enter card details.
    /// Call this before or on the due date to generate a payment link for rent.
    /// </summary>
    Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        Guid paymentId,
        decimal amount,
        string? description = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Returned by CreatePaymentIntentAsync — pass clientSecret and publishableKey
/// to the Angular frontend so it can complete the payment with Stripe.js.
/// </summary>
public sealed record CreatePaymentIntentResult(
    string ClientSecret,
    string PaymentIntentId,
    string PublishableKey
);

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
