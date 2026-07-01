using Identity.Application.Interfaces;
using Microsoft.Extensions.Options;
using Maintenance.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Exceptions;
using Payment.Application.Interfaces;
using Property.Application.Features.Queries.GetPropertyById;
using Payment.Application.Features.Commands.ApplyLateFee;
using Payment.Application.Features.Commands.MarkAsPaidExternally;
using Payment.Application.Features.Commands.GenerateRentPaymentLink;
using Payment.Application.Features.Commands.WaiveLateFee;
using Payment.Application.Features.Commands.CreateSubscriptionPayment;
using Payment.Application.Features.Commands.GenerateLeasePayment;
using Payment.Application.Features.Commands.GenerateMonthlyPayments;
using Payment.Application.Features.Commands.ProcessPayment;
using Payment.Application.Features.Commands.RefundPayment;
using Payment.Application.Features.Commands.UpdateAiPrediction;
using Payment.Application.Features.Queries.GetAllRentPayments;
using Payment.Application.Features.Queries.GetFinancialSummary;
using Payment.Application.Features.Queries.GetOwnerFinancialSummary;
using Payment.Application.Features.Queries.GetMyRentPayments;
using Payment.Application.Features.Queries.GetOverduePayments;
using Payment.Application.Features.Queries.GetPaymentReceipt;
using Payment.Application.Features.Queries.GetPaymentsByStatus;
using Payment.Application.Features.Queries.GetRentPaymentById;
using Payment.Infrastructure.BackgroundJobs;
using Payment.Infrastructure.Services.Stripe;
using Wallet.Application.Interfaces;
using Wallet.Domain.Enums;

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
    private readonly IPaymentGatewayService _paymentGateway;

    public PaymentController(
        IMediator mediator,
        ICurrentUserService currentUser,
        IUserRepository userRepo,
        StripeWebhookService stripeWebhook,
        IPaymentGatewayService paymentGateway)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _userRepo = userRepo;
        _stripeWebhook = stripeWebhook;
        _paymentGateway = paymentGateway;
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

    /// <summary>
    /// Confirms a rent Checkout Session from the success redirect — marks the
    /// rent payment as Paid and queues the wallet-credit event via Outbox.
    /// Idempotent (already-Paid payments are skipped). Called by the Angular
    /// success page with the ?session_id=... from the Stripe redirect URL.
    /// </summary>
    [HttpPost("rent/confirm")]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> ConfirmRentPayment(
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
            p.CreatedAt, p.UpdatedAt,
            p.CheckoutUrl, p.CheckoutSessionId
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

    /// <summary>
    /// Generates a Stripe PaymentIntent for an existing rent payment record and
    /// returns the clientSecret + publishableKey so the Angular frontend can render
    /// a Stripe Elements form. Works before, on, and after the due date — tenants
    /// can pay early. Each call creates a new PaymentIntent (idempotent: Stripe
    /// de-duplicates by idempotency key "pay_{id}").
    /// </summary>
    [HttpPost("{id:guid}/initiate")]
    [Authorize(Roles = "tenant,owner,company_manager")]
    public async Task<ActionResult<CreatePaymentIntentResult>> InitiatePayment(
        Guid id, CancellationToken cancellationToken)
    {
        var payment = await _mediator.Send(new GetRentPaymentByIdQuery(id), cancellationToken);
        if (payment is null)
            return NotFound(new { error = $"Payment {id} not found." });

        if (payment.Status == "Paid")
            return BadRequest(new { error = "This payment has already been completed." });

        try
        {
            var result = await _paymentGateway.CreatePaymentIntentAsync(
                id,
                payment.Amount + payment.LateFee,
                $"Rent — lease {payment.LeaseId} due {payment.DueDate:yyyy-MM-dd}",
                cancellationToken);

            return Ok(result);
        }
        catch (Stripe.StripeException ex)
        {
            return BadRequest(new { error = ex.StripeError?.Message ?? ex.Message });
        }
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

    [HttpGet("owner/financial-summary")]
    [Authorize(Roles = "owner,company_manager,admin")]
    public async Task<ActionResult> GetOwnerFinancialSummary(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");
        var result = await _mediator.Send(new GetOwnerFinancialSummaryQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Comprehensive platform-level financial dashboard.
    /// Aggregates rent commission (1%), maintenance commission (2%), and withdrawal commission (1%).
    /// </summary>
    [HttpGet("platform-dashboard")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GetPlatformDashboard(
        [FromServices] IMaintenancePaymentRepository maintenancePaymentRepo,
        [FromServices] IWithdrawalRepository withdrawalRepo,
        CancellationToken ct)
    {
        // ── Rent ──────────────────────────────────────────────────────────────
        var rentSummary = await _mediator.Send(new GetFinancialSummaryQuery(), ct);

        var totalRentCollected  = rentSummary.TotalCollected;
        var totalRentCommission = rentSummary.TotalRentCommission;
        var pendingRentCount    = rentSummary.TotalOverduePayments;
        var pendingRentAmount   = rentSummary.TotalPending;

        // ── Maintenance ───────────────────────────────────────────────────────
        var paidMaintenancePayments = await maintenancePaymentRepo.GetAllPaidAsync(ct);
        var totalMaintenanceCollected  = paidMaintenancePayments.Sum(p => p.Amount);
        var totalMaintenanceCommission = paidMaintenancePayments.Sum(p => p.CommissionAmount);
        var totalMaintenanceNet        = paidMaintenancePayments.Sum(p => p.NetAmount);

        // ── Withdrawals ───────────────────────────────────────────────────────
        var allWithdrawals = await withdrawalRepo.GetAllAsync(ct);
        var completedWithdrawals       = allWithdrawals.Where(w => w.Status == WithdrawalStatus.Completed).ToList();
        var pendingWithdrawals         = allWithdrawals.Where(w => w.Status == WithdrawalStatus.Pending).ToList();
        var totalWithdrawalAmount      = completedWithdrawals.Sum(w => w.Amount);
        var totalWithdrawalCommission  = allWithdrawals.Sum(w => w.CommissionAmount);
        var totalWithdrawalNet         = completedWithdrawals.Sum(w => w.NetAmount);

        // ── Platform totals ───────────────────────────────────────────────────
        var totalPlatformCommission = totalRentCommission + totalMaintenanceCommission + totalWithdrawalCommission;

        return Ok(new
        {
            rent = new
            {
                totalCollected        = totalRentCollected,
                totalCommission       = totalRentCommission,        // 1%
                commissionRate        = "1%",
                totalPending          = pendingRentAmount,
                overdueCount          = pendingRentCount,
                totalLateFees         = rentSummary.TotalLateFees
            },
            maintenance = new
            {
                totalCollected        = totalMaintenanceCollected,
                totalCommission       = totalMaintenanceCommission, // 2%
                commissionRate        = "2%",
                totalNetToVendors     = totalMaintenanceNet,
                paidPaymentsCount     = paidMaintenancePayments.Count
            },
            withdrawals = new
            {
                totalWithdrawn        = totalWithdrawalAmount,
                totalCommission       = totalWithdrawalCommission,  // 1%
                commissionRate        = "1%",
                totalNetPaidOut       = totalWithdrawalNet,
                pendingCount          = pendingWithdrawals.Count,
                pendingAmount         = pendingWithdrawals.Sum(w => w.Amount),
                completedCount        = completedWithdrawals.Count
            },
            platform = new
            {
                totalCommissionEarned = totalPlatformCommission,
                breakdown = new
                {
                    fromRent          = totalRentCommission,
                    fromMaintenance   = totalMaintenanceCommission,
                    fromWithdrawals   = totalWithdrawalCommission
                }
            }
        });
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

    /// <summary>
    /// Admin: marks a rent payment as settled outside the platform (cash / real-world).
    /// Sets Status = PaidExternally, excludes it from the overdue queue, and blocks
    /// future late-fee application.  Typical use: the first installment of a newly
    /// registered lease that the tenant already paid before the system was set up.
    /// </summary>
    [HttpPost("{id:guid}/mark-external")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> MarkAsPaidExternally(
        Guid id,
        [FromBody] MarkExternalRequestBody? body,
        CancellationToken cancellationToken)
    {
        var success = await _mediator.Send(
            new MarkAsPaidExternallyCommand(id, body?.Notes),
            cancellationToken);

        return success ? Ok() : NotFound(new { error = $"Payment {id} not found." });
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

    /// <summary>
    /// Waives the late fee on a rent payment. The Stripe checkout link is
    /// regenerated at the base amount (1% commission only, no late fee).
    /// Owner: can only waive their own property's payments.
    /// Company manager: can waive any payment managed by their company.
    /// Admin: unrestricted.
    /// </summary>
    [HttpPost("{id:guid}/waive-late-fee")]
    [Authorize(Roles = "owner,company_manager,admin")]
    public async Task<ActionResult> WaiveLateFee(
        Guid id,
        [FromServices] IOptions<RentLinkJobSettings> jobSettings,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role   = _currentUser.Role   ?? string.Empty;

        var success = await _mediator.Send(new WaiveLateFeeCommand(
            PaymentId:    id,
            RequesterId:  userId,
            RequesterRole: role,
            SuccessUrl:   jobSettings.Value.SuccessUrl,
            CancelUrl:    jobSettings.Value.CancelUrl
        ), cancellationToken);

        return success ? Ok() : BadRequest(new { error = "Late fee could not be waived. Check that you are the landlord and the payment has an active late fee." });
    }

    /// <summary>
    /// Admin-only: generates (or regenerates) a Stripe checkout link for a rent payment.
    /// Use this when the background job hasn't run yet or for demo/testing purposes.
    /// </summary>
    [HttpPost("{id:guid}/generate-link")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> GenerateRentPaymentLink(
        Guid id,
        [FromServices] IOptions<RentLinkJobSettings> jobSettings,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GenerateRentPaymentLinkCommand(
            PaymentId:  id,
            SuccessUrl: jobSettings.Value.SuccessUrl,
            CancelUrl:  jobSettings.Value.CancelUrl
        ), cancellationToken);

        return result.Success
            ? Ok(new { checkoutUrl = result.CheckoutUrl })
            : BadRequest(new { error = result.Error });
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
public sealed record MarkExternalRequestBody(string? Notes);
public sealed record ProcessPaymentRequestBody(string PaymentMethod, string? CardToken);
public sealed record RefundPaymentRequestBody(string Reason);
public sealed record ApplyLateFeeRequestBody(decimal LateFeeAmount);
public sealed record UpdateAiPredictionRequestBody(bool PredictedLate);
record RefundRequest(string Reason);
