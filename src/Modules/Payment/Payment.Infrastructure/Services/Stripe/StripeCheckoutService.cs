using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Settings;
using Stripe;
using Stripe.Checkout;

namespace Payment.Infrastructure.Services.Stripe;

/// <summary>
/// Real Stripe Checkout implementation
///
/// إزاي مختلف عن StripePaymentGatewayService؟
/// - PaymentIntent: أنت بتبني الـ payment form بنفسك (مناسب للإيجار — frontend مخصص)
/// - Checkout Session: Stripe بيستضيف صفحة دفع كاملة جاهزة (أسرع وأسهل للاشتراكات)
/// </summary>
public sealed class StripeCheckoutService : IStripeCheckoutService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeCheckoutService> _logger;

    public StripeCheckoutService(
        IOptions<StripeSettings> settings,
        ILogger<StripeCheckoutService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;

        // تحويل المبلغ لـ smallest currency unit (AED → fils: × 100)
        var amountInSmallestUnit = (long)(request.Amount * 100);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _settings.DefaultCurrency,
                        UnitAmount = amountInSmallestUnit,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName
                        }
                    },
                    Quantity = 1
                }
            },
            // Stripe substitutes {CHECKOUT_SESSION_ID} on redirect. Use the right
            // separator so session_id is a real query param even when SuccessUrl
            // already carries one (e.g. ?checkout=success).
            SuccessUrl = $"{request.SuccessUrl}{(request.SuccessUrl.Contains('?') ? "&" : "?")}session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = request.CancelUrl,
            Metadata = request.Metadata,

            // مهم جداً: ندمج الـ metadata في الـ PaymentIntent الناتج عن الـ Session
            // عشان لو حصل أي event على payment_intent (مش بس checkout.session)
            // يكون فيه payment_id في الـ metadata برضو
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Metadata = request.Metadata
            }
        };

        // Idempotency Key: لو نفس الـ request اتبعت مرتين، Stripe بيرجع نفس الـ Session
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = $"checkout_{request.PaymentId}"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, requestOptions, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Stripe Checkout Session created. SessionId={SessionId} PaymentId={PaymentId} Url={Url}",
            session.Id, request.PaymentId, session.Url);

        return new CheckoutSessionResult(session.Id, session.Url);
    }

    public async Task<CheckoutSessionStatus> GetSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;

        var service = new SessionService();
        var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

        return new CheckoutSessionStatus(
            SessionId: session.Id,
            PaymentStatus: session.PaymentStatus,
            PaymentIntentId: session.PaymentIntentId);
    }
}
