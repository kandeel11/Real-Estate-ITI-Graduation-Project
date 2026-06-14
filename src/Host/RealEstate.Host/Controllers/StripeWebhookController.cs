using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Services.Stripe;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/stripe")]
public sealed class StripeWebhookController : ControllerBase
{
    private readonly StripeWebhookService _webhookService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        StripeWebhookService webhookService,
        ILogger<StripeWebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
    {
        string payload;
        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        var stripeSignature = Request.Headers["Stripe-Signature"];

        if (string.IsNullOrEmpty(stripeSignature))
        {
            _logger.LogWarning("Stripe webhook received without signature header.");
            return BadRequest("Missing Stripe-Signature header.");
        }

        try
        {
            await _webhookService.HandleWebhookAsync(payload, stripeSignature!, cancellationToken);
            return Ok(new { received = true });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("signature"))
        {
            _logger.LogWarning("Invalid Stripe webhook signature attempt.");
            return BadRequest("Invalid signature.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook.");
            return Ok(new { received = true, warning = "Processing error logged." });
        }
    }

    [HttpPost("create-payment-intent/{paymentId:guid}")]
    public async Task<IActionResult> CreatePaymentIntent(
        Guid paymentId,
        [FromServices] IRentPaymentRepository repository, 
        [FromServices] StripePaymentGatewayService stripeService,
        CancellationToken cancellationToken)
    {
        var payment = await repository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        if (payment.Status == Payment.Domain.Enums.PaymentStatus.Paid)
            return BadRequest(new { message = "Payment already completed." });

        var result = await stripeService.CreatePaymentIntentAsync(
            paymentId: payment.Id,
            amount: payment.Amount + payment.LateFee,
            description: $"Rent payment for {payment.DueDate:MMMM yyyy}",
            cancellationToken: cancellationToken
        );

        return Ok(new
        {
            clientSecret = result.ClientSecret,
            paymentIntentId = result.PaymentIntentId,
            publishableKey = result.PublishableKey,
            amount = payment.Amount + payment.LateFee,
            currency = "aed"
        });
    }
}
