using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Services.Stripe;

namespace RealEstate.Host.Controllers;

/// <summary>
/// Controller بيستقبل Webhook events من Stripe
/// 
/// خطوات الإعداد على Stripe Dashboard:
/// 1. روح لـ Developers → Webhooks
/// 2. اضغط "Add endpoint"
/// 3. الـ URL: https://your-domain.com/api/stripe/webhook
/// 4. اختار الـ events:
///    - payment_intent.succeeded
///    - payment_intent.payment_failed
///    - charge.refunded
/// 5. انسخ الـ "Signing secret" وحطه في appsettings.json → Stripe:WebhookSecret
/// 
/// مهم جداً: الـ endpoint ده لازم يكون بدون Authentication
/// علشان Stripe نفسه هو اللي بيبعت الـ requests
/// </summary>
[ApiController]
[Route("api/payments/webhook")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly StripeWebhookService _webhookService;

    public StripeWebhookController(StripeWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signatureHeader))
            return BadRequest(new { error = "Missing Stripe-Signature header." });

        try
        {
            await _webhookService.HandleWebhookAsync(payload, signatureHeader, cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex);
            // فشل التحقق من الـ signature أو الـ payload — Stripe هيعيد المحاولة تلقائياً
            // لو رجعنا غير 2xx، فمن المهم نرجع 400 بدل 500 عشان منمنعش الـ retries المفيدة
            return BadRequest(new { error = ex.Message });
        }
    }
}
