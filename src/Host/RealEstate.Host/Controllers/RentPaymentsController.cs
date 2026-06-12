using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Features.Commands.ApplyLateFee;
using Payment.Application.Features.Commands.GenerateMonthlyPayments;
using Payment.Application.Features.Commands.ProcessPayment;
using Payment.Application.Features.Commands.RefundPayment;
using Payment.Application.Features.Commands.UpdateAiPrediction;
using Payment.Application.Features.Queries.GetAllRentPayments;
using Payment.Application.Features.Queries.GetFinancialSummary;
using Payment.Application.Features.Queries.GetOverduePayments;
using Payment.Application.Features.Queries.GetPaymentsByStatus;
using Payment.Application.Features.Queries.GetRentPaymentById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api")]
public sealed class RentPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RentPaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("rent-payments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? leaseId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusResult = await _mediator.Send(
                new GetPaymentsByStatusQuery(status), cancellationToken);
            return Ok(statusResult);
        }

        var result = await _mediator.Send(
            new GetAllRentPaymentsQuery(leaseId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("rent-payments/overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOverduePaymentsQuery(), cancellationToken);
        return Ok(result);
    }


    [HttpGet("rent-payments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRentPaymentByIdQuery(id), cancellationToken);

        return result is null
            ? NotFound(new { message = $"Payment {id} not found." })
            : Ok(result);
    }

    [HttpPost("leases/{leaseId:guid}/rent-payments/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateMonthlyPayments(
        Guid leaseId,
        [FromBody] GeneratePaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateMonthlyPaymentsCommand(request?.TargetMonth), cancellationToken);
        return Ok(result);
    }

    [HttpPost("rent-payments/{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        Guid id,
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ProcessPaymentCommand(id, request.PaymentMethod, request.CardToken),
            cancellationToken);

        return result.IsSuccess ? Ok(result) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPost("rent-payments/{id:guid}/apply-late-fee")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyLateFee(
        Guid id,
        [FromBody] ApplyLateFeeRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new ApplyLateFeeCommand(id, request.LateFeeAmount), cancellationToken);

        return success ? Ok(new { message = "Late fee applied." }) : NotFound();
    }

    [HttpPut("rent-payments/{id:guid}/ai-predicted-late")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAiPrediction(
        Guid id,
        [FromBody] UpdateAiPredictionRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new UpdateAiPredictionCommand(id, request.PredictedLate), cancellationToken);

        return success ? Ok(new { message = "AI prediction updated." }) : NotFound();
    }

    [HttpPost("rent-payments/{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refund(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new RefundPaymentCommand(id, request.Reason), cancellationToken);

        return success ? Ok(new { message = "Payment refunded." }) : BadRequest(new { message = "Cannot refund this payment." });
    }

    [HttpGet("reports/financial-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinancialSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFinancialSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}

public sealed record GeneratePaymentsRequest(DateOnly? TargetMonth);
public sealed record ProcessPaymentRequest(string PaymentMethod, string? CardToken = null);
public sealed record ApplyLateFeeRequest(decimal LateFeeAmount);
public sealed record UpdateAiPredictionRequest(bool PredictedLate);
public sealed record RefundRequest(string Reason);
