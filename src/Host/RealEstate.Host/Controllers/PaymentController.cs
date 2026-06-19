using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Exceptions;
using Payment.Application.Features.Commands.ApplyLateFee;
using Payment.Application.Features.Commands.CreateSubscriptionPayment;
using Payment.Application.Features.Commands.GenerateLeasePayment;
using Payment.Application.Features.Commands.GenerateMonthlyPayments;
using Payment.Application.Features.Commands.ProcessPayment;
using Payment.Application.Features.Commands.RefundPayment;
using Payment.Application.Features.Commands.UpdateAiPrediction;
using Payment.Application.Features.Queries.GetAllRentPayments;
using Payment.Application.Features.Queries.GetFinancialSummary;
using Payment.Application.Features.Queries.GetOverduePayments;
using Payment.Application.Features.Queries.GetPaymentReceipt;
using Payment.Application.Features.Queries.GetPaymentsByStatus;
using Payment.Application.Features.Queries.GetRentPaymentById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator)
    {
        _mediator = mediator;
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

    // ── Rent Payments ────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult> GetAllRentPayments(
        [FromQuery] Guid? leaseId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllRentPaymentsQuery(leaseId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRentPaymentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentsByStatusQuery(status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("overdue")]
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
    public async Task<ActionResult> Refund(
        Guid id,
        [FromBody] RefundPaymentRequestBody body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new RefundPaymentCommand(id, body.Reason), cancellationToken);
        return success ? Ok() : BadRequest(new { error = "Payment could not be refunded." });
    }

    [HttpPost("{id:guid}/late-fee")]
    public async Task<ActionResult> ApplyLateFee(
        Guid id,
        [FromBody] ApplyLateFeeRequestBody body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(new ApplyLateFeeCommand(id, body.LateFeeAmount), cancellationToken);
        return success ? Ok() : BadRequest(new { error = "Late fee could not be applied." });
    }

    [HttpPost("{id:guid}/ai-prediction")]
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
public sealed record ProcessPaymentRequestBody(string PaymentMethod, string? CardToken);
public sealed record RefundPaymentRequestBody(string Reason);
public sealed record ApplyLateFeeRequestBody(decimal LateFeeAmount);
public sealed record UpdateAiPredictionRequestBody(bool PredictedLate);
record RefundRequest(string Reason);
