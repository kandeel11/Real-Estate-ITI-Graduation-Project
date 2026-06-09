using Document.Application.Commands.Lease.ConfirmLease;
using Document.Application.Commands.Lease.TerminateLease;
using Document.Application.Commands.Lease.UploadLeaseContract;
using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leases")]
public class LeasesController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("upload")]
    [Authorize(Roles = "owner,company_manager,broker")]
    public async Task<IActionResult> Upload([FromForm] Guid leadId, IFormFile file, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;   // FIX: was Guid.NewGuid()
        if (ownerId is null) return Unauthorized();
        var result = await mediator.Send(new UploadLeaseContractCommand(leadId, ownerId.Value, file), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lease = await mediator.Send(new GetLeaseByIdQuery(id), ct);
        if (lease is null) return NotFound();

        var userId = currentUser.UserId;
        if (!currentUser.IsInRole("admin") && lease.TenantId != userId && lease.OwnerId != userId)
            return Forbid();

        return Ok(lease);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "owner,company_manager,broker")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmLeaseRequest req, CancellationToken ct)
    {
        var ownerId = currentUser.UserId;   // FIX: was Guid.NewGuid()
        if (ownerId is null) return Unauthorized();
        await mediator.Send(new ConfirmLeaseCommand(
            id, ownerId.Value,
            req.RentAmount, req.SecurityDeposit,
            req.StartDate, req.EndDate, req.PaymentDayOfMonth), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] TerminateRequest req, CancellationToken ct)
    {
        await mediator.Send(new TerminateLeaseCommand(id, req.Reason), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var lease = await mediator.Send(new GetLeaseByIdQuery(id), ct);
        if (lease is null) return NotFound();

        var userId = currentUser.UserId;
        if (!currentUser.IsInRole("admin") && lease.TenantId != userId && lease.OwnerId != userId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(lease.ContractFileUrl))
            return NotFound(new { message = "Contract file not available." });

        return Ok(new { downloadUrl = lease.ContractFileUrl });
    }
}

public record ConfirmLeaseRequest(decimal? RentAmount, decimal? SecurityDeposit, DateOnly? StartDate, DateOnly? EndDate, int? PaymentDayOfMonth);
public record TerminateRequest(string Reason);