using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using Payment.Infrastructure.Settings;
using Stripe;

namespace Payment.Infrastructure.Services.Stripe;

public sealed class StripeWebhookService
{
    private readonly IRentPaymentRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILeaseService _leaseService;
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IRentPaymentRepository repository,
        INotificationService notificationService,
        ILeaseService leaseService,
        IOptions<StripeSettings> settings,
        ILogger<StripeWebhookService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _leaseService = leaseService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task HandleWebhookAsync(
        string payload,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
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

        await (stripeEvent.Type switch
        {
            "payment_intent.succeeded"      => HandlePaymentSucceededAsync(stripeEvent, cancellationToken),
            "payment_intent.payment_failed" => HandlePaymentFailedAsync(stripeEvent, cancellationToken),
            "charge.refunded"               => HandleChargeRefundedAsync(stripeEvent, cancellationToken),
            _                               => Task.CompletedTask  
        });
    }

    private async Task HandlePaymentSucceededAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            _logger.LogWarning("payment_intent.succeeded: could not cast event data.");
            return;
        }
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

        await TrySendPaymentConfirmationAsync(payment, intent.Id, receiptUrl, ct);
    }

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

        await TrySendPaymentFailedNotificationAsync(payment, failureMessage, ct);
    }

    private async Task HandleChargeRefundedAsync(Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Charge charge) return;

        var paymentIntentId = charge.PaymentIntentId;
        if (string.IsNullOrEmpty(paymentIntentId)) return;

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

    private async Task TrySendPaymentConfirmationAsync(
        Domain.Entities.RentPayment payment,
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
        Domain.Entities.RentPayment payment,
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
