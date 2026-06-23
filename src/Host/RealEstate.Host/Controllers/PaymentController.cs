using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Exceptions;
using Property.Application.Features.Queries.GetPropertyById;
using Payment.Application.Features.Commands.ApplyLateFee;
using Payment.Application.Features.Commands.CreateSubscriptionPayment;
using Payment.Application.Features.Commands.GenerateLeasePayment;
using Payment.Application.Features.Commands.GenerateMonthlyPayments;
using Payment.Application.Features.Commands.ProcessPayment;
using Payment.Application.Features.Commands.RefundPayment;
using Payment.Application.Features.Commands.UpdateAiPrediction;
using Payment.Application.Features.Queries.GetAllRentPayments;
using Payment.Application.Features.Queries.GetFinancialSummary;
using Payment.Application.Features.Queries.GetMyRentPayments;
using Payment.Application.Features.Queries.GetOverduePayments;
using Payment.Application.Features.Queries.GetPaymentReceipt;
using Payment.Application.Features.Queries.GetPaymentsByStatus;
using Payment.Application.Features.Queries.GetRentPaymentById;
using Payment.Infrastructure.Services.Stripe;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepo;
    private readonly StripeWebhookService _stripeWebhook;

    public PaymentController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IUserRepository userRepo,
        StripeWebhookService stripeWebhook)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _userRepo = userRepo;
        _stripeWebhook = stripeWebhook;
    }

    // ── Subscription Payments ───────────────────────────────────

    /// <summary>
    /// يبدأ عملية دفع اشتراك (جديد/تجديد/ترقية) ويرجع Stripe Checkout URL
    /// خطوة 2 في Subscription Payment Flow (بعد SelectPlanCommand في Subscription module)
    /// </summary>
    [HttpPost("subscriptions/checkout")]
    public async Task<ActionResult<CreateSubscriptionPaymentResult>> CreateSubscriptionPayment(
        [FromBody] CreateSubscriptionPaymentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (PlanNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirms a subscription Checkout Session from the success redirect — for
    /// local/dev use when the Stripe webhook isn't forwarded. Verifies the
    /// session is paid (server-side, via Stripe) then runs the activation path.
    /// Idempotent and safe to call even if the webhook already processed it.
    /// </summary>
    [HttpPost("subscriptions/confirm")]
    public async Task<IActionResult> ConfirmSubscriptionPayment(
        [FromBody] ConfirmSubscriptionPaymentRequest body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.SessionId))
            return BadRequest(new { error = "sessionId is required." });

        try
        {
            var paymentStatus = await _stripeWebhook.ConfirmCheckoutSessionAsync(body.SessionId, cancellationToken);
            return Ok(new { paymentStatus });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Rent Payments ────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GetAllRentPayments(
        [FromQuery] Guid? leaseId,
        CancellationToken cancellationToken)
    {
        var payments = await _mediator.Send(new GetAllRentPaymentsQuery(leaseId), cancellationToken);

        // Enrich with property title and tenant name — batch lookups to avoid N+1.
        var propertyIds = payments.Select(p => p.PropertyId).Distinct().ToList();
        var tenantIds   = payments.Select(p => p.TenantId).Distinct().ToList();

        var propertyTitles = new Dictionary<Guid, string>();
        foreach (var pid in propertyIds)
        {
            var prop = await _mediator.Send(new GetPropertyByIdQuery(pid), cancellationToken);
            if (prop is not null) propertyTitles[pid] = prop.Title;
        }

        var tenantNames = await _userRepo.GetFullNamesByIdsAsync(tenantIds, cancellationToken);

        var result = payments.Select(p => new
        {
            p.Id, p.LeaseId, p.PropertyId, p.TenantId, p.LandlordId,
            propertyTitle = propertyTitles.GetValueOrDefault(p.PropertyId, string.Empty),
            tenantName    = tenantNames.GetValueOrDefault(p.TenantId, string.Empty),
            p.Amount, p.DueDate, p.PaidAt, p.Status,
            p.PaymentMethod, p.TransactionId, p.LateFee,
            p.AiPredictedLate, p.ReceiptUrl, p.Notes,
            p.CreatedAt, p.UpdatedAt
        });

        return Ok(result);
    }

    /// <summary>
    /// Returns all rent payments for the currently logged-in user.
    /// Tenants see payments where they are the payer; owners see payments for their properties.
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult> GetMyPayments(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var role = _currentUser.Role ?? "tenant";
        var result = await _mediator.Send(new GetMyRentPaymentsQuery(userId, role), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRentPaymentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("status/{status}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentsByStatusQuery(status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("overdue")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GetOverdue(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOverduePaymentsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult> GetReceipt(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetPaymentReceiptQuery(id), cancellationToken);
            return Ok(result);
        }
        catch (PaymentNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("summary")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GetFinancialSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFinancialSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/process")]
    public async Task<ActionResult<ProcessPaymentResult>> Process(
        Guid id,
        [FromBody] ProcessPaymentRequestBody body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProcessPaymentCommand(id, body.PaymentMethod, body.CardToken),
            cancellationToken);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Refund(
        Guid id,
        [FromBody] RefundPaymentRequestBody body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RefundPaymentCommand(id, body.Reason), cancellationToken);
        return success ? Ok() : BadRequest(new { error = "Payment could not be refunded." });
    }

    [HttpPost("{id:guid}/late-fee")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> ApplyLateFee(
        Guid id,
        [FromBody] ApplyLateFeeRequestBody body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new ApplyLateFeeCommand(id, body.LateFeeAmount), cancellationToken);
        return success ? Ok() : BadRequest(new { error = "Late fee could not be applied." });
    }

    [HttpPost("{id:guid}/ai-prediction")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> UpdateAiPrediction(
        Guid id,
        [FromBody] UpdateAiPredictionRequestBody body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new UpdateAiPredictionCommand(id, body.PredictedLate), cancellationToken);

        return success ? Ok() : NotFound();
    }

    /// <summary>
    /// يولّد دفعات الإيجار الشهرية — بيتم استدعاؤه عادة من MonthlyPaymentGenerationJob
    /// (Hosted Service) لكنه متاح هنا كـ manual trigger للأدمن
    /// </summary>
    [HttpPost("generate-monthly")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GenerateMonthlyPaymentsResult>> GenerateMonthlyPayments(
        [FromQuery] DateOnly? targetMonth,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateMonthlyPaymentsCommand(targetMonth), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// يولّد دفعة الفترة الحالية لـ lease معين من قاعدة البيانات.
    /// يُستخدم لو الدفعة مش اتولدت تلقائياً — بيجيب بيانات الـ lease من الـ DB الحقيقية.
    /// </summary>
    [HttpPost("generate-for-lease/{leaseId:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GenerateLeasePaymentResult>> GenerateForLease(
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GenerateLeasePaymentCommand(leaseId), cancellationToken);
            return result.Generated ? Ok(result) : Conflict(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

// ── Request bodies (تبسيط الـ binding بدل ما الـ route param يتكرر في الـ command) ──
public sealed record ConfirmSubscriptionPaymentRequest(string SessionId);
public sealed record ProcessPaymentRequestBody(string PaymentMethod, string? CardToken);
public sealed record RefundPaymentRequestBody(string Reason);
public sealed record ApplyLateFeeRequestBody(decimal LateFeeAmount);
public sealed record UpdateAiPredictionRequestBody(bool PredictedLate);
record RefundRequest(string Reason);
