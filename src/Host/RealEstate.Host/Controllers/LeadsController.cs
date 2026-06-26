using Document.Application.Commands.Lead.CloseNoManagement;
using Document.Application.Commands.Lead.ManagerConfirmNoManagement;
using Document.Application.Commands.Lead.UpdateLeadStatus;
using Document.Application.Queries.Lead.GetAllLeads;
using Document.Application.Queries.Lead.GetLeadsByBroker;
using Identity.Application.Interfaces;
using Identity.Application.Queries.GetCompanyManagerId;
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

    /// <summary>
    /// Closes a deal without a lease or management service.
    /// Independent broker (no companyId): property is rented immediately, tenant notified.
    /// Company broker (has companyId): company manager is notified for confirmation first.
    /// </summary>
    [HttpPost("{id}/close-no-management")]
    [Authorize(Roles = "broker")]
    public async Task<IActionResult> CloseNoManagement(Guid id, CancellationToken ct)
    {
        try
        {
            // Resolve company manager only for company brokers
            Guid? managerId = null;
            var companyId = _currentUser.CompanyId;
            if (companyId.HasValue)
            {
                managerId = await _mediator.Send(
                    new GetCompanyManagerIdQuery(companyId.Value), ct);
            }

            var result = await _mediator.Send(
                new CloseNoManagementCommand(id, managerId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Company manager confirms a broker's "no management" close request.
    /// Finalizes the deal: marks property as Rented and notifies the tenant.
    /// </summary>
    [HttpPost("{id}/manager-confirm-no-management")]
    [Authorize(Roles = "company_manager,admin")]
    public async Task<IActionResult> ManagerConfirmNoManagement(Guid id, CancellationToken ct)
    {
        var managerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();
        try
        {
            await _mediator.Send(
                new ManagerConfirmNoManagementCommand(id, managerId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)       { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)  { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex){ return StatusCode(403, new { error = ex.Message }); }
    }
}

public record UpdateLeadStatusRequest(
    string NewStatus,
    decimal? DealValue = null,
    string? Notes = null);
