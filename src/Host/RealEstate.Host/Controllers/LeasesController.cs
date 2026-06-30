using Document.Application.Commands.Lease.ApproveLeaseManagement;
using Document.Application.Commands.Lease.TenantAcceptLease;
using Document.Application.Commands.Lease.ConfirmLease;
using Document.Application.Commands.Lease.RenewLease;
using Document.Application.Commands.Lease.RequestTermination;
using Document.Application.Commands.Lease.RespondToTermination;
using Document.Application.Commands.Lease.SendLeaseToTenant;
using Document.Application.Commands.Lease.SetLeaseManagementOptions;
using Document.Application.Commands.Lease.TerminateLease;
using Document.Application.Commands.Lease.UploadLeaseContract;
using Document.Application.Interfaces;
using Document.Application.Queries.Lease;
using Document.Application.Queries.Lease.GetLeaseById;
using Document.Application.Queries.Lease.GetLeasePaymentSchedule;
using Identity.Application.Interfaces;
using Identity.Application.Queries.GetCompanyManagerId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leases")]
public class LeasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAzureBlobService _blob;
    private readonly ILogger<LeasesController> _logger;

    public LeasesController(
        IMediator mediator,
        IUserRepository userRepo,
        ICurrentUserService currentUser,
        IAzureBlobService blob,
        ILogger<LeasesController> logger)
    {
        _mediator = mediator;
        _userRepo = userRepo;
        _currentUser = currentUser;
        _blob = blob;
        _logger = logger;
    }

    private Guid CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] Guid leadId, IFormFile file, CancellationToken ct)
    {
        if (leadId == Guid.Empty) return BadRequest(new { error = "leadId is required." });
        if (file is null || file.Length == 0) return BadRequest(new { error = "A PDF file is required." });

        try
        {
            // Pass the uploader's ID as BrokerId when they are a broker so the lease
            // knows who uploaded it (needed for the manager → tenant routing later).
            var brokerId = _currentUser.IsInRole("broker") ? CurrentUserId : (Guid?)null;
            var result = await _mediator.Send(new UploadLeaseContractCommand(leadId, Guid.Empty, file, brokerId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var lease = await _mediator.Send(new GetLeaseByIdQuery(id));
        if (lease == null) return NotFound();
        return Ok(lease);
    }

    /// <summary>AI-extracted lease terms, shaped for the broker's lease-upload review step.</summary>
    [HttpGet("{id}/ai-extraction")]
    public async Task<IActionResult> GetAiExtraction(Guid id, CancellationToken ct)
    {
        var lease = await _mediator.Send(new GetLeaseByIdQuery(id), ct);
        if (lease == null) return NotFound();

        _logger.LogInformation(
            "[LeaseExtraction] GET ai-extraction for LeaseId={LeaseId} | Status={Status} | Confidence={Conf}% | " +
            "InstallmentAmount={IA} | PaymentFrequency={PF} | StartDate={ST} | EndDate={ED} | " +
            "DurationInMonths={DM} | TotalRentAmount={TR} | SecurityDeposit={SD} | PaymentDayOfMonth={PD}",
            id, lease.ExtractionStatus, lease.ExtractionConfidence,
            lease.InstallmentAmount, lease.PaymentFrequency,
            lease.StartDate, lease.EndDate,
            lease.DurationInMonths, lease.TotalRentAmount,
            lease.SecurityDeposit, lease.PaymentDayOfMonth);

        if (lease.InstallmentAmount == null || lease.StartDate == null)
            _logger.LogWarning(
                "[LeaseExtraction] LeaseId={LeaseId} — frontend will receive incomplete data. " +
                "InstallmentAmount={IA}, StartDate={ST}, ExtractionStatus={Status}",
                id, lease.InstallmentAmount, lease.StartDate, lease.ExtractionStatus);

        var tenant = await _userRepo.GetByIdAsync(lease.TenantId, ct);
        var property = await _mediator.Send(new GetPropertyByIdQuery(lease.PropertyId), ct);

        return Ok(new
        {
            tenantName        = tenant?.FullName ?? string.Empty,
            tenantEmail       = tenant?.Email ?? string.Empty,
            propertyTitle     = property?.Title ?? string.Empty,
            startDate         = lease.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            endDate           = lease.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            installmentAmount = lease.InstallmentAmount ?? 0m,
            paymentFrequency  = lease.PaymentFrequency,
            totalRentAmount   = lease.TotalRentAmount,
            durationInMonths  = lease.DurationInMonths,
            deposit           = lease.SecurityDeposit ?? 0m,
            extractionStatus  = MapExtractionStatus(lease.ExtractionStatus),
            confidence        = lease.ExtractionConfidence ?? 0m
        });
    }

    /// <summary>
    /// Debug endpoint — returns the full extraction state including which fields are
    /// null and why. Use this to diagnose why the frontend isn't getting data.
    /// </summary>
    [HttpGet("{id}/extraction-debug")]
    public async Task<IActionResult> GetExtractionDebug(Guid id, CancellationToken ct)
    {
        var lease = await _mediator.Send(new GetLeaseByIdQuery(id), ct);
        if (lease == null) return NotFound(new { error = $"Lease {id} not found." });

        _logger.LogInformation(
            "[LeaseExtraction] extraction-debug called for LeaseId={LeaseId}", id);

        var nullFields = new List<string>();
        if (lease.InstallmentAmount == null) nullFields.Add("installmentAmount");
        if (lease.PaymentFrequency == null)  nullFields.Add("paymentFrequency");
        if (lease.StartDate == null)         nullFields.Add("startDate");
        if (lease.EndDate == null)           nullFields.Add("endDate");
        if (lease.DurationInMonths == null)  nullFields.Add("durationInMonths");
        if (lease.TotalRentAmount == null)   nullFields.Add("totalRentAmount");
        if (lease.SecurityDeposit == null)   nullFields.Add("securityDeposit");
        if (lease.PaymentDayOfMonth == null) nullFields.Add("paymentDayOfMonth");

        var status = MapExtractionStatus(lease.ExtractionStatus);
        var isReady = status == "done" && nullFields.Count == 0;

        return Ok(new
        {
            leaseId          = lease.Id,
            extractionStatus = lease.ExtractionStatus,
            displayStatus    = status,
            confidence       = lease.ExtractionConfidence ?? 0m,
            isReadyForFrontend = isReady,
            nullFields,
            diagnosis = nullFields.Count == 0
                ? "All required fields are populated."
                : $"These fields are null — extraction may have failed or the PDF didn't contain them: [{string.Join(", ", nullFields)}]",
            fields = new
            {
                installmentAmount   = lease.InstallmentAmount,
                paymentFrequency    = lease.PaymentFrequency,
                totalRentAmount     = lease.TotalRentAmount,
                durationInMonths    = lease.DurationInMonths,
                securityDeposit     = lease.SecurityDeposit,
                startDate           = lease.StartDate?.ToString("yyyy-MM-dd"),
                endDate             = lease.EndDate?.ToString("yyyy-MM-dd"),
                paymentDayOfMonth   = lease.PaymentDayOfMonth
            },
            meta = new
            {
                leaseStatus  = lease.Status,
                isActive     = lease.IsActive,
                createdAt    = lease.CreatedAt,
                tenantId     = lease.TenantId,
                propertyId   = lease.PropertyId
            }
        });
    }

    /// <summary>Persist reviewed lease terms. Company brokers route to manager approval first.</summary>
    [HttpPost("{id}/send-to-tenant")]
    public async Task<IActionResult> SendToTenant(Guid id, [FromBody] SendLeaseToTenantRequest req, CancellationToken ct)
    {
        try
        {
            // Company brokers: look up the manager and route through them.
            Guid? managerId = null;
            if (_currentUser.IsInRole("broker") && _currentUser.CompanyId.HasValue)
                managerId = await _mediator.Send(new GetCompanyManagerIdQuery(_currentUser.CompanyId.Value), ct);

            await _mediator.Send(new SendLeaseToTenantCommand(
                id,
                req.InstallmentAmount,
                req.Deposit,
                DateOnly.TryParse(req.StartDate, out var s) ? s : null,
                DateOnly.TryParse(req.EndDate, out var e) ? e : null,
                req.PaymentFrequency,
                req.TotalRentAmount,
                req.DurationInMonths,
                req.PaymentDayOfMonth,
                managerId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private static string MapExtractionStatus(string? status) => status switch
    {
        "extracted" or "confirmed" => "done",
        "failed" or "rejected" => "failed",
        _ => "pending"
    };

    [HttpDelete("{id}")]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] string reason)
    {
        await _mediator.Send(new TerminateLeaseCommand(id, reason));
        return NoContent();
    }

    // ── Early termination (mutual consent) ──────────────────────────────────

    /// <summary>Either party proposes ending the lease early; the other party must approve.</summary>
    [HttpPost("{id}/terminate-request")]
    [Authorize]
    public async Task<IActionResult> RequestTermination(Guid id, [FromBody] TerminationRequestBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest(new { error = "A reason is required." });
        try
        {
            await _mediator.Send(new RequestLeaseTerminationCommand(
                id, CurrentUserId, body.Reason,
                DateOnly.TryParse(body.EffectiveDate, out var d) ? d : null), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>The counterparty approves a pending termination request — ends the lease.</summary>
    [HttpPost("{id}/terminate-approve")]
    [Authorize]
    public async Task<IActionResult> ApproveTermination(Guid id, [FromBody] TerminationResponseBody? body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RespondToLeaseTerminationCommand(id, CurrentUserId, true, body?.Note), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>The counterparty rejects a pending termination request — lease continues.</summary>
    [HttpPost("{id}/terminate-reject")]
    [Authorize]
    public async Task<IActionResult> RejectTermination(Guid id, [FromBody] TerminationResponseBody? body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RespondToLeaseTerminationCommand(id, CurrentUserId, false, body?.Note), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Renewal (landlord uploads a new contract, same AI cycle) ─────────────

    /// <summary>
    /// Owner/broker uploads a fresh contract to renew an existing lease. Returns the
    /// new lease id + extraction status; the caller then polls {newId}/ai-extraction
    /// and finishes with {newId}/send-to-tenant — identical to a first-time upload.
    /// </summary>
    [HttpPost("{id}/renew-upload")]
    [Authorize]
    public async Task<IActionResult> RenewUpload(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { error = "A PDF file is required." });
        try
        {
            var result = await _mediator.Send(new RenewLeaseUploadCommand(id, CurrentUserId, file), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    // ── Full-management workflow (Company Broker only) ────────────────────────

    /// <summary>
    /// Broker sets which management services are included for this lease.
    /// Full management (Maintenance + PaymentCollection selected) routes to tenant directly.
    /// Partial selection routes to the company manager for approval first.
    /// </summary>
    [HttpPost("{id:guid}/management-options")]
    [Authorize]
    public async Task<IActionResult> SetManagementOptions(
        Guid id, [FromBody] SetManagementOptionsRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SetLeaseManagementOptionsCommand(id, req.ManagementOptions ?? [], req.ManagerUserId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Company manager approves or rejects a broker's management option selection.
    /// Approved → broker can send the lease to the tenant.
    /// Rejected → broker is notified to revise the options.
    /// </summary>
    [HttpPost("{id:guid}/manager-approve")]
    [Authorize]
    public async Task<IActionResult> ApproveManagement(
        Guid id, [FromBody] ManagerApprovalRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ApproveLeaseManagementCommand(id, CurrentUserId, req.IsApproved, req.Notes), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Tenant accepts the lease offer — activates the lease and triggers the payment schedule.
    /// Only the assigned tenant may call this. The lease must be in "extracted" status
    /// (i.e. the manager has approved and forwarded it to the tenant).
    /// </summary>
    [HttpPost("{id:guid}/tenant-accept")]
    [Authorize]
    public async Task<IActionResult> TenantAccept(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new TenantAcceptLeaseCommand(id, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmLeaseRequest req)
    {
        var result = await _mediator.Send(new ConfirmLeaseCommand(
            id, req.ConfirmerId,
            req.InstallmentAmount, req.PaymentFrequency, req.TotalRentAmount, req.DurationInMonths,
            req.SecurityDeposit, req.StartDate, req.EndDate, req.PaymentDayOfMonth));

        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaseRequest req)
    {
        var result = await _mediator.Send(new RejectLeaseCommand(id, req.TenantId, req.Reason));
        return Ok(result);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var lease = await _mediator.Send(new GetLeaseByIdQuery(id));
        if (lease == null) return NotFound();
        if (string.IsNullOrWhiteSpace(lease.ContractFileUrl))
            return NotFound(new { error = "No contract file is attached to this lease." });

        // ContractFileUrl is stored as a blob name (e.g. "leases/abc.pdf"). Hand the
        // browser a time-limited SAS URL it can open directly. Tolerate values that are
        // already absolute URLs (older rows / external links).
        var url = Uri.IsWellFormedUriString(lease.ContractFileUrl, UriKind.Absolute)
            ? lease.ContractFileUrl
            : _blob.GenerateSasUrl(lease.ContractFileUrl);
        return Ok(new { contractFileUrl = url });
    }

    /// <summary>
    /// Returns the full payment schedule for a lease: every installment period with
    /// start/end/due dates and amount, plus which period is current, which is next,
    /// and a summary of total/due-so-far/remaining amounts.
    /// Returns 404 if the lease doesn't exist.
    /// Returns 422 if the lease exists but extraction is incomplete (no installment/frequency/duration).
    /// </summary>
    [HttpGet("{id}/payment-schedule")]
    public async Task<IActionResult> GetPaymentSchedule(Guid id, CancellationToken ct)
    {
        var schedule = await _mediator.Send(new GetLeasePaymentScheduleQuery(id), ct);

        if (schedule is null)
        {
            var lease = await _mediator.Send(new GetLeaseByIdQuery(id), ct);
            return lease is null
                ? NotFound(new { error = "Lease not found." })
                : UnprocessableEntity(new { error = "Payment schedule is not available yet — lease extraction is incomplete." });
        }

        return Ok(schedule);
    }
}

public record ConfirmLeaseRequest(
    Guid ConfirmerId,
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth);

public record RejectLeaseRequest(Guid TenantId, string Reason);

public record SetManagementOptionsRequest(
    List<string>? ManagementOptions,  // e.g. ["Maintenance", "PaymentCollection"]
    Guid? ManagerUserId               // company manager's UserId for notification routing
);

public record ManagerApprovalRequest(bool IsApproved, string? Notes);

public record TerminationRequestBody(string Reason, string? EffectiveDate);
public record TerminationResponseBody(string? Note);

public record SendLeaseToTenantRequest(
    decimal? InstallmentAmount = null,
    decimal? Deposit = null,
    string? StartDate = null,
    string? EndDate = null,
    string? PaymentFrequency = null,
    decimal? TotalRentAmount = null,
    int? DurationInMonths = null,
    int? PaymentDayOfMonth = null,
    string? TenantEmail = null,
    string? TenantName = null);
