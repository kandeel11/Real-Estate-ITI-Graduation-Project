namespace Payment.Application.Interfaces;

/// <summary>
/// Contract لإنشاء Stripe Checkout Sessions
///
/// مختلف عن IPaymentGatewayService (اللي بيستخدم PaymentIntent للإيجار):
/// - PaymentIntent: أنت بتبني الـ payment form بنفسك (مناسب للإيجار — frontend مخصص)
/// - Checkout Session: Stripe بيستضيف صفحة دفع كاملة جاهزة (أسرع وأسهل للاشتراكات)
/// </summary>
public interface IStripeCheckoutService
{
    /// <summary>
    /// ينشئ Checkout Session جديدة ويرجع الرابط اللي يتم تحويل المستخدم له
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// يجيب حالة Checkout Session معينة — مفيد للـ polling أو verification
    /// </summary>
    Task<CheckoutSessionStatus> GetSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateCheckoutSessionRequest(
    Guid PaymentId,
    decimal Amount,
    string ProductName,
    string SuccessUrl,
    string CancelUrl,
    Dictionary<string, string> Metadata,
    string? IdempotencySuffix = null
);

public sealed record CheckoutSessionResult(
    string SessionId,
    string Url
);

public sealed record CheckoutSessionStatus(
    string SessionId,
    string PaymentStatus,   // "paid", "unpaid", "no_payment_required"
    string? PaymentIntentId
);
