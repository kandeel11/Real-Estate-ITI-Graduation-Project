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
}

public record ConfirmLeaseRequest(
    Guid ConfirmerId,
    decimal? RentAmount,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth);

public record RejectLeaseRequest(Guid TenantId, string Reason);