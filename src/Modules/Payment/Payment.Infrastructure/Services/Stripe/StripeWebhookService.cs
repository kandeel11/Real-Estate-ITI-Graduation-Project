using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using Payment.Infrastructure.Settings;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Stripe;
using Stripe.Checkout;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Infrastructure.Services.Stripe;

/// <summary>
/// بيستقبل ويعالج الـ Webhook events القادمة من Stripe
///
/// ليه محتاجين Webhook؟
/// لأن الدفع ببطاقة بياخد وقت أحياناً (3D Secure, bank verification, Checkout redirect)
/// Stripe بيبعت Webhook لما الدفع يتأكد فعلاً
///
/// الـ Events اللي بنستمع ليها:
/// - payment_intent.succeeded       → الدفع نجح (Rent) ✅
/// - payment_intent.payment_failed  → الدفع فشل ❌
/// - charge.refunded                → الاسترداد تم 🔄
/// - checkout.session.completed     → الدفع نجح (Subscription عبر Checkout) ✅
/// - checkout.session.expired       → جلسة الدفع انتهت بدون دفع ⌛
/// </summary>
public sealed class StripeWebhookService
{
    private readonly IPaymentRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILeaseService _leaseService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IPaymentRepository repository,
        INotificationService notificationService,
        ILeaseService leaseService,
        IOutboxWriter outboxWriter,
        IOptions<StripeSettings> settings,
        ILogger<StripeWebhookService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _leaseService = leaseService;
        _outboxWriter = outboxWriter;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// نقطة الدخول الرئيسية — بتستقبل الـ raw payload من الـ controller
    /// وبتتحقق من الـ signature علشان تتأكد إن الـ event جاي من Stripe فعلاً
    /// </summary>
    public async Task HandleWebhookAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        // التحقق من الـ Stripe Signature — أهم خطوة أمنية
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                stripeSignatureHeader,
                _settings.WebhookSecret
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Invalid Stripe webhook signature.");
            throw new InvalidOperationException("Invalid Stripe webhook signature.", ex);
        }

        _logger.LogInformation("Received Stripe webhook event: {EventType} Id={EventId}",
            stripeEvent.Type, stripeEvent.Id);

        // روّح للـ handler المناسب بناءً على نوع الـ event
        await (stripeEvent.Type switch
        {
            "payment_intent.succeeded"      => HandlePaymentSucceededAsync(stripeEvent, cancellationToken),
            "payment_intent.payment_failed" => HandlePaymentFailedAsync(stripeEvent, cancellationToken),
            "charge.refunded"                => HandleChargeRefundedAsync(stripeEvent, cancellationToken),
            "checkout.session.completed"     => HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken),
            "checkout.session.expired"       => HandleCheckoutSessionExpiredAsync(stripeEvent, cancellationToken),
            _                               => Task.CompletedTask  // نتجاهل الـ events التانية
        });
    }

    // ── payment_intent.succeeded (Rent — PaymentIntent flow) ──
    private async Task HandlePaymentSucceededAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            _logger.LogWarning("payment_intent.succeeded: could not cast event data.");
            return;
        }

        // جيب الـ payment_id اللي حطيناه في الـ metadata
        if (!intent.Metadata.TryGetValue("payment_id", out var paymentIdStr)
            || !Guid.TryParse(paymentIdStr, out var paymentId))
        {
            _logger.LogWarning("payment_intent.succeeded: missing payment_id in metadata. IntentId={Id}", intent.Id);
            return;
        }

        var payment = await _repository.GetByIdAsync(paymentId, ct);
        if (payment is null)
        {
            _logger.LogWarning("payment_intent.succeeded: Payment {PaymentId} not found.", paymentId);
            return;
        }

        // تجنب التكرار — لو اتعمل process قبل كده
        if (payment.Status == PaymentStatus.Paid)
        {
            _logger.LogInformation("Payment {PaymentId} already marked as Paid. Skipping.", paymentId);
            return;
        }

        var receiptUrl = intent.LatestCharge?.ReceiptUrl;
        payment.MarkAsPaid(
            transactionId: intent.Id,
            paymentMethod: intent.PaymentMethodTypes.FirstOrDefault() ?? "card",
            receiptUrl: receiptUrl
        );

        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment {PaymentId} marked as Paid via Stripe Webhook. IntentId={IntentId}",
            paymentId, intent.Id);

        // إرسال إيميل للمستأجر — بس لو دي دفعة إيجار (عندها Lease)
        if (payment.PaymentType == PaymentType.Rent)
        {
            await TrySendPaymentConfirmationAsync(payment, intent.Id, receiptUrl, ct);

            if (payment.RentDetails is not null)
            {
                var details = payment.RentDetails;
                await _outboxWriter.AddAsync(
                    new RentPaymentSucceededEvent(
                        PaymentId: payment.Id,
                        OwnerId: details.LandlordId,
                        TenantId: details.TenantId,
                        LeaseId: details.LeaseId,
                        PropertyId: details.PropertyId,
                        Amount: payment.Amount
                    ),
                    ct);
                await _repository.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "RentPaymentSucceededEvent queued in outbox for Payment {PaymentId}, Owner {OwnerId}",
                    payment.Id, details.LandlordId);
            }
        }
    }

    // ── checkout.session.completed (Subscription — Checkout flow) ──
    //
    // دي أهم خطوة في الـ Subscription Payment Flow:
    // 1. payment.MarkPaid()
    // 2. Publish SubscriptionPaymentSucceededEvent (عبر Outbox)
    // 3. Subscription module بيستقبل الـ event ويعمل subscription.Activate()
    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Session session)
        {
            _logger.LogWarning("checkout.session.completed: could not cast event data.");
            return;
        }

        if (!session.Metadata.TryGetValue("payment_id", out var paymentIdStr)
            || !Guid.TryParse(paymentIdStr, out var paymentId))
        {
            _logger.LogWarning("checkout.session.completed: missing payment_id. SessionId={Id}", session.Id);
            return;
        }

        var payment = await _repository.GetByIdAsync(paymentId, ct);
        if (payment is null)
        {
            _logger.LogWarning("checkout.session.completed: Payment {PaymentId} not found.", paymentId);
            return;
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            _logger.LogInformation("Payment {PaymentId} already Paid. Skipping.", paymentId);
            return;
        }

        if (session.PaymentStatus != "paid")
        {
            _logger.LogWarning("checkout.session.completed: PaymentStatus={Status} for SessionId={SessionId}",
                session.PaymentStatus, session.Id);
            return;
        }

        var paymentIntentId = session.PaymentIntentId ?? session.Id;

        payment.MarkAsPaid(
            transactionId: paymentIntentId,
            paymentMethod: "card",
            receiptUrl: null);

        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment {PaymentId} marked Paid via Checkout. SessionId={SessionId}",
            paymentId, session.Id);

        // ── Publish Integration Event via Outbox (Subscription Activation) ──
        if (payment.PaymentType == PaymentType.Subscription && payment.SubscriptionDetails is not null)
        {
            var details = payment.SubscriptionDetails;

            // الـ UserId مش مخزّن في SubscriptionPaymentDetails (modular boundary) —
            // بنجيبه من الـ Stripe metadata اللي حطيناه وقت إنشاء الـ Checkout Session
            var userId = session.Metadata.TryGetValue("user_id", out var userIdStr)
                && Guid.TryParse(userIdStr, out var parsedUserId)
                ? parsedUserId
                : Guid.Empty;

            await _outboxWriter.AddAsync(
                new SubscriptionPaymentSucceededEvent(
                    SubscriptionId: details.SubscriptionId,
                    PaymentId: payment.Id,
                    UserId: userId,
                    PlanId: details.PlanId,
                    Amount: payment.Amount
                ),
                ct);

            // الـ outbox message محتاجة SaveChanges منفصل (نفس DbContext، نفس الـ scope)
            // عشان تُحفظ كصف فعلي في الجدول
            await _repository.SaveChangesAsync(ct);

            _logger.LogInformation(
                "SubscriptionPaymentSucceededEvent queued in outbox for Payment {PaymentId}, Subscription {SubscriptionId}",
                payment.Id, details.SubscriptionId);
        }
    }

    // ── checkout.session.expired (Subscription — abandoned checkout) ──
    private async Task HandleCheckoutSessionExpiredAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Session session) return;

        if (!session.Metadata.TryGetValue("payment_id", out var paymentIdStr)
            || !Guid.TryParse(paymentIdStr, out var paymentId))
            return;

        var payment = await _repository.GetByIdAsync(paymentId, ct);
        if (payment is null || payment.Status == PaymentStatus.Paid) return;

        payment.MarkAsFailed();
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Checkout session expired for Payment {PaymentId}. Marked as Failed.", paymentId);
    }

    // ── payment_intent.payment_failed ─────────────────────────
    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent) return;

        if (!intent.Metadata.TryGetValue("payment_id", out var paymentIdStr)
            || !Guid.TryParse(paymentIdStr, out var paymentId))
            return;

        var payment = await _repository.GetByIdAsync(paymentId, ct);
        if (payment is null || payment.Status == PaymentStatus.Failed) return;

        payment.MarkAsFailed();
        await _repository.SaveChangesAsync(ct);

        var failureMessage = intent.LastPaymentError?.Message ?? "Payment failed";

        _logger.LogWarning(
            "Payment {PaymentId} marked as Failed via Stripe Webhook. Reason={Reason}",
            paymentId, failureMessage);

        // إرسال إيميل للمستأجر بالفشل — بس لو دي دفعة إيجار
        if (payment.PaymentType == PaymentType.Rent)
        {
            await TrySendPaymentFailedNotificationAsync(payment, failureMessage, ct);
        }
    }

    // ── charge.refunded ───────────────────────────────────────
    private async Task HandleChargeRefundedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Charge charge) return;

        // جيب الـ payment_id من metadata الـ PaymentIntent
        var paymentIntentId = charge.PaymentIntentId;
        if (string.IsNullOrEmpty(paymentIntentId)) return;

        // ابحث عن الدفعة بالـ TransactionId (اللي هو الـ PaymentIntent ID)
        var allPayments = await _repository.GetAllAsync(ct);
        var payment = allPayments.FirstOrDefault(p => p.TransactionId == paymentIntentId);

        if (payment is null)
        {
            _logger.LogWarning("charge.refunded: No payment found for IntentId={IntentId}", paymentIntentId);
            return;
        }

        payment.MarkAsRefunded("Refunded via Stripe");
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment {PaymentId} marked as Refunded via Stripe Webhook.",
            payment.Id);
    }

    // ── Notification Helpers (Rent only) ──────────────────────
    private async Task TrySendPaymentConfirmationAsync(
        PaymentEntity payment,
        string transactionId,
        string? receiptUrl,
        CancellationToken ct)
    {
        try
        {
            var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
            if (lease is null) return;

            var body = $"""
                <div style="font-family: Arial, sans-serif; direction: rtl;">
                    <h2 style="color: #2e7d32;">✅ تم استلام دفعتك بنجاح</h2>
                    <p>عزيزي <strong>{lease.TenantName}</strong>،</p>
                    <p>تم استلام دفعة الإيجار الخاصة بك بنجاح.</p>
                    <table style="border-collapse: collapse; width: 100%;">
                        <tr><td><strong>المبلغ:</strong></td><td>{payment.Amount:N2} AED</td></tr>
                        <tr><td><strong>رقم المعاملة:</strong></td><td>{transactionId}</td></tr>
                        <tr><td><strong>تاريخ الدفع:</strong></td><td>{payment.PaidAt:dd/MM/yyyy HH:mm} UTC</td></tr>
                    </table>
                    {(receiptUrl is not null ? $"<p><a href='{receiptUrl}' style='color: #1976d2;'>📄 تحميل الإيصال</a></p>" : "")}
                    <p style="color: #666; font-size: 12px;">شكراً لالتزامك بالدفع في موعده.</p>
                </div>
                """;

            await _notificationService.SendEmailAsync(new(
                ToEmail: lease.TenantEmail,
                ToName: lease.TenantName,
                Subject: $"✅ تأكيد استلام دفعة الإيجار - {payment.DueDate:MMMM yyyy}",
                Body: body
            ), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation email for {PaymentId}", payment.Id);
        }
    }

    private async Task TrySendPaymentFailedNotificationAsync(
        PaymentEntity payment,
        string reason,
        CancellationToken ct)
    {
        try
        {
            var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
            if (lease is null) return;

            var body = $"""
                <div style="font-family: Arial, sans-serif; direction: rtl;">
                    <h2 style="color: #c62828;">❌ فشل في معالجة دفعتك</h2>
                    <p>عزيزي <strong>{lease.TenantName}</strong>،</p>
                    <p>للأسف، تعذّر معالجة دفعة الإيجار الخاصة بك.</p>
                    <p><strong>السبب:</strong> {reason}</p>
                    <p>يرجى التواصل معنا أو المحاولة مرة أخرى.</p>
                </div>
                """;

            await _notificationService.SendEmailAsync(new(
                ToEmail: lease.TenantEmail,
                ToName: lease.TenantName,
                Subject: "❌ فشل دفعة الإيجار — يرجى المراجعة",
                Body: body
            ), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failed email for {PaymentId}", payment.Id);
        }
    }
}
