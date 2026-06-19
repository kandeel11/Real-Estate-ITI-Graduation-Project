using Document.Application.Commands.Lead.UpdateLeadStatus;
using Document.Application.Queries.Lead.GetAllLeads;
using Document.Application.Queries.Lead.GetLeadsByBroker;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize(Roles = "owner,broker,company_manager,admin")]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public LeadsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Admin: all leads with optional status filter and pagination.
    /// Brokers should use GET /api/leads/broker/{brokerId} for their own leads.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var leads = await _mediator.Send(new GetAllLeadsQuery(page, pageSize, status), ct);
        return Ok(leads);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLeadStatusRequest req)
    {
        try
        {
            await _mediator.Send(new UpdateLeadStatusCommand(
                id, req.NewStatus, req.DealValue, req.Notes));
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("broker/{brokerId}")]
    public async Task<IActionResult> GetByBroker(Guid brokerId)
    {
        // Brokers can only read their OWN leads; managers/admins can read any
        if (_currentUser.IsInRole("broker") && _currentUser.UserId != brokerId)
            return Forbid();

        var leads = await _mediator.Send(new GetLeadsByBrokerQuery(brokerId));
        return Ok(leads);
    }
}

public record UpdateLeadStatusRequest(
    string NewStatus,
    decimal? DealValue = null,
    string? Notes = null);
