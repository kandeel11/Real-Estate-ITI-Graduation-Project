using Document.Application.Commands.Lease;
using Document.Application.Queries.Lease;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leases")]
public class LeasesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeasesController(IMediator mediator) => _mediator = mediator;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] Guid leadId, IFormFile file)
    {
        var ownerId = Guid.NewGuid(); // مؤقتاً للتيست
        var leaseId = await _mediator.Send(new UploadLeaseContractCommand(leadId, ownerId, file));
        return Ok(new { leaseId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var lease = await _mediator.Send(new GetLeaseByIdQuery(id));
        if (lease == null) return NotFound();
        return Ok(lease);
    }

    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var ownerId = Guid.NewGuid(); // مؤقتاً للتيست
        await _mediator.Send(new ConfirmLeaseCommand(id, ownerId));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] string reason)
    {
        await _mediator.Send(new TerminateLeaseCommand(id, reason));
        return NoContent();
    }
}