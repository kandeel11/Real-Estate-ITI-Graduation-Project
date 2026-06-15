using Document.Application.Commands.Lease.ConfirmLease;
using Document.Application.Commands.Lease.SendLeaseToTenant;
using Document.Application.Commands.Lease.TerminateLease;
using Document.Application.Commands.Lease.UploadLeaseContract;
using Document.Application.Queries.Lease;
using Document.Application.Queries.Lease.GetLeaseById;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertyById;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leases")]
public class LeasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepo;
    public LeasesController(IMediator mediator, IUserRepository userRepo)
    {
        _mediator = mediator;
        _userRepo = userRepo;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] Guid leadId, IFormFile file)
    {
        if (leadId == Guid.Empty) return BadRequest(new { error = "leadId is required." });
        if (file is null || file.Length == 0) return BadRequest(new { error = "A PDF file is required." });

        try
        {
            // The owner is derived from the lead inside the handler; OwnerId here is ignored.
            var result = await _mediator.Send(new UploadLeaseContractCommand(leadId, Guid.Empty, file));
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

        var tenant = await _userRepo.GetByIdAsync(lease.TenantId, ct);
        var property = await _mediator.Send(new GetPropertyByIdQuery(lease.PropertyId), ct);

        return Ok(new
        {
            tenantName = tenant?.FullName ?? string.Empty,
            tenantEmail = tenant?.Email ?? string.Empty,
            propertyTitle = property?.Title ?? string.Empty,
            startDate = lease.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            endDate = lease.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            monthlyRent = lease.RentAmount ?? 0m,
            deposit = lease.SecurityDeposit ?? 0m,
            extractionStatus = MapExtractionStatus(lease.ExtractionStatus),
            confidence = lease.ExtractionConfidence ?? 0m
        });
    }

    /// <summary>Persist reviewed lease terms and notify the tenant a lease is ready.</summary>
    [HttpPost("{id}/send-to-tenant")]
    public async Task<IActionResult> SendToTenant(Guid id, [FromBody] SendLeaseToTenantRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SendLeaseToTenantCommand(
                id,
                req.MonthlyRent,
                req.Deposit,
                DateOnly.TryParse(req.StartDate, out var s) ? s : null,
                DateOnly.TryParse(req.EndDate, out var e) ? e : null), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Backend stores pending/extracted/confirmed/failed/rejected; the chat/lease UI
    // only distinguishes pending → done → failed.
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
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmLeaseRequest req)
    {
        var result = await _mediator.Send(new ConfirmLeaseCommand(
        id, req.ConfirmerId,
        req.RentAmount, req.SecurityDeposit,
        req.StartDate, req.EndDate,
        req.PaymentDayOfMonth));

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
        return Ok(new { contractFileUrl = lease.ContractFileUrl });
    }
}

public record ConfirmLeaseRequest(
    Guid ConfirmerId,
    decimal? RentAmount,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth);

public record RejectLeaseRequest(Guid TenantId, string Reason);

public record SendLeaseToTenantRequest(
    decimal? MonthlyRent = null,
    decimal? Deposit = null,
    string? StartDate = null,
    string? EndDate = null,
    string? TenantEmail = null,
    string? TenantName = null);