using Document.Application.Commands.Lead.UpdateLeadStatus;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeadsController(IMediator mediator) => _mediator = mediator;

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLeadStatusRequest req)
    {
        await _mediator.Send(new UpdateLeadStatusCommand(
            id, req.NewStatus, req.DealValue, req.Notes));
        return NoContent();
    }
}

public record UpdateLeadStatusRequest(
    string NewStatus,
    decimal? DealValue = null,
    string? Notes = null);