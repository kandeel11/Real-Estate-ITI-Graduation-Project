using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Settings;
using Stripe;

namespace Payment.Infrastructure.Services.Stripe;

/// <summary>
/// Real Stripe implementation للـ IPaymentGatewayService
/// بيستخدم Stripe PaymentIntents API
/// 
/// إزاي بيشتغل:
/// 1. بنعمل PaymentIntent بالمبلغ والعملة
/// 2. Stripe بيرجع client_secret للـ frontend
/// 3. الـ frontend بيكمل الدفع بالـ card details
/// 4. Stripe بيبعت Webhook لما الدفع يتأكد
/// 
/// ملاحظة: المبلغ في Stripe بيتبعت بالـ smallest currency unit
/// مثلاً 100 AED = 10000 (بالفلس/هللة)
/// </summary>
public sealed class StripePaymentGatewayService : IPaymentGatewayService
{
    private readonly StripeSettings _settings;
    private readonly string _appBaseUrl;
    private readonly ILogger<StripePaymentGatewayService> _logger;

    public StripePaymentGatewayService(
        IOptions<StripeSettings> settings,
        IConfiguration configuration,
        ILogger<StripePaymentGatewayService> logger)
    {
        _settings = settings.Value;
        _appBaseUrl = configuration["EmailSettings:AppBaseUrl"]?.TrimEnd('/') ?? "https://localhost:7240";
        _logger = logger;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating Stripe PaymentIntent for PaymentId={PaymentId} Amount={Amount} {Currency}",
                request.PaymentId, request.Amount, _settings.DefaultCurrency);

            // تحويل المبلغ لـ smallest unit (AED → fils: × 100)
            var amountInSmallestUnit = (long)(request.Amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInSmallestUnit,
                Currency = _settings.DefaultCurrency,
                PaymentMethod = request.CardToken, // الـ PaymentMethod ID من الـ frontend
                Confirm = true,                    // اعمل confirm فوراً
                ReturnUrl = $"{_appBaseUrl}/payment/complete",
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = request.PaymentId.ToString(),
                    ["description"] = request.Description ?? "Rent Payment"
                },
                Description = request.Description ?? $"Rent payment - {request.PaymentId}",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never" // مش محتاجين redirect في الـ API
                }
            };

            var service = new PaymentIntentService();

            // Idempotency Key: لو نفس الـ request اتبعت مرتين بسبب network retry
            // Stripe بيرجع نفس الـ PaymentIntent بدل ما يعمل charge جديد
            var requestOptions = new RequestOptions { IdempotencyKey = $"pay_{request.PaymentId}" };
            var paymentIntent = await service.CreateAsync(options, requestOptions, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Stripe PaymentIntent created. IntentId={IntentId} Status={Status}",
                paymentIntent.Id, paymentIntent.Status);

            // لو الـ status = succeeded → الدفع اتم بنجاح
            if (paymentIntent.Status == "succeeded")
            {
                return new PaymentGatewayResult(
                    IsSuccess: true,
                    TransactionId: paymentIntent.Id,
                    ReceiptUrl: paymentIntent.LatestCharge?.ReceiptUrl,
                    ErrorMessage: null
                );
            }

            // لو محتاج action تاني (مثلاً 3D Secure)
            if (paymentIntent.Status == "requires_action")
            {
                _logger.LogWarning(
                    "Stripe PaymentIntent {IntentId} requires additional action (3D Secure).",
                    paymentIntent.Id);

                return new PaymentGatewayResult(
                    IsSuccess: false,
                    TransactionId: paymentIntent.Id,
                    ReceiptUrl: null,
                    ErrorMessage: $"requires_action:{paymentIntent.NextAction?.Type}"
                );
            }

            // أي status تاني = فشل
            return new PaymentGatewayResult(
                IsSuccess: false,
                TransactionId: paymentIntent.Id,
                ReceiptUrl: null,
                ErrorMessage: $"PaymentIntent status: {paymentIntent.Status}"
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe error for PaymentId={PaymentId}. Code={Code} Message={Message}",
                request.PaymentId, ex.StripeError?.Code, ex.Message);

            return new PaymentGatewayResult(
                IsSuccess: false,
                TransactionId: null,
                ReceiptUrl: null,
                ErrorMessage: ex.StripeError?.Message ?? ex.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment {PaymentId}", request.PaymentId);

            return new PaymentGatewayResult(
                IsSuccess: false,
                TransactionId: null,
                ReceiptUrl: null,
                ErrorMessage: "An unexpected error occurred while processing payment."
            );
        }
    }

    /// <summary>
    /// إنشاء PaymentIntent بدون Confirm — بيرجع client_secret للـ frontend
    /// الـ frontend يستخدمه مع Stripe.js لإكمال الدفع
    /// </summary>
    public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        Guid paymentId,
        decimal amount,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;

            var amountInSmallestUnit = (long)(amount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInSmallestUnit,
                Currency = _settings.DefaultCurrency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = paymentId.ToString()
                },
                Description = description ?? $"Rent payment - {paymentId}"
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Created PaymentIntent {IntentId} for PaymentId={PaymentId}",
                intent.Id, paymentId);

            return new CreatePaymentIntentResult(
                ClientSecret: intent.ClientSecret,
                PaymentIntentId: intent.Id,
                PublishableKey: _settings.PublishableKey
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create PaymentIntent for {PaymentId}", paymentId);
            throw;
        }
    }

    /// <summary>
    /// عمل Refund لدفعة عبر Stripe
    /// </summary>
    public async Task<PaymentGatewayResult> RefundAsync(
        string transactionId,
        decimal? amount = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            StripeConfiguration.ApiKey = _settings.SecretKey;

            var options = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = amount.HasValue ? (long?)(amount.Value * 100) : null, // null = full refund
                Reason = reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                }
            };

            var service = new RefundService();
            var refund = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Stripe Refund created. RefundId={RefundId} Status={Status}",
                refund.Id, refund.Status);

            return new PaymentGatewayResult(
                IsSuccess: refund.Status == "succeeded" || refund.Status == "pending",
                TransactionId: refund.Id,
                ReceiptUrl: null,
                ErrorMessage: refund.Status != "succeeded" && refund.Status != "pending"
                    ? $"Refund status: {refund.Status}" : null
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe Refund failed for TransactionId={TransactionId}", transactionId);

            return new PaymentGatewayResult(
                IsSuccess: false,
                TransactionId: null,
                ReceiptUrl: null,
                ErrorMessage: ex.StripeError?.Message ?? ex.Message
            );
        }
    }
}

