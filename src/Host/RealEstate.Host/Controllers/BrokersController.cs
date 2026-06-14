using Identity.Application.Commands.AddBroker;
using Identity.Application.Commands.DeactivateBroker;
using Identity.Application.Commands.DeleteBroker;
using Identity.Application.Commands.ReactivateBroker;
using Identity.Application.Commands.UpdateBrokerScore;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/brokers")]
[Authorize(Policy = "CompanyManager")]
public class BrokersController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Add a broker to the CURRENT manager's own company — companyId is taken from the JWT,
    /// so the caller never has to supply it. This is the normal company-manager path.
    /// </summary>
    [HttpPost("~/api/companies/mine/brokers")]
    public async Task<IActionResult> AddBrokerToMyCompany(
        [FromBody] AddBrokerRequest request,
        CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "You are not associated with a company." });

        return await CreateBrokerAsync(companyId.Value, request, ct);
    }

    /// <summary>
    /// Add a broker to a specific company by id. Managers may only target their own company;
    /// admins may target any. Kept mainly for admin cross-company use.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddBroker(
        Guid companyId,
        [FromBody] AddBrokerRequest request,
        CancellationToken ct)
    {
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != companyId)
            return Forbid();

        return await CreateBrokerAsync(companyId, request, ct);
    }

    /// <summary>
    /// Deactivate a broker in the current manager's own company (removes them + blocks their login).
    /// </summary>
    [HttpDelete("~/api/companies/mine/brokers/{brokerId:guid}")]
    public async Task<IActionResult> DeactivateMyBroker(Guid brokerId, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null && !currentUser.IsInRole("admin"))
            return BadRequest(new { error = "You are not associated with a company." });

        try
        {
            await mediator.Send(new DeactivateBrokerCommand(brokerId, companyId, currentUser.IsInRole("admin")), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>
    /// Reactivate a previously deactivated broker (restores their login).
    /// Re-checks the plan's broker limit before re-enabling the seat.
    /// </summary>
    [HttpPost("~/api/companies/mine/brokers/{brokerId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateMyBroker(Guid brokerId, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null && !currentUser.IsInRole("admin"))
            return BadRequest(new { error = "You are not associated with a company." });

        try
        {
            await mediator.Send(new ReactivateBrokerCommand(brokerId, companyId, currentUser.IsInRole("admin")), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>
    /// Permanently delete a broker. Their properties are NOT touched — the
    /// manager reassigns them via PATCH /api/properties/{id}/assign-broker.
    /// </summary>
    [HttpDelete("~/api/companies/mine/brokers/{brokerId:guid}/permanent")]
    public async Task<IActionResult> DeleteMyBroker(Guid brokerId, CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null && !currentUser.IsInRole("admin"))
            return BadRequest(new { error = "You are not associated with a company." });

        try
        {
            await mediator.Send(new DeleteBrokerCommand(brokerId, companyId, currentUser.IsInRole("admin")), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private async Task<IActionResult> CreateBrokerAsync(Guid companyId, AddBrokerRequest request, CancellationToken ct)
    {
        try
        {
            var cmd = new AddBrokerCommand(
                CompanyId: companyId,
                FullName: request.FullName,
                Email: request.Email,
                TempPassword: request.TempPassword,
                Phone: request.Phone,
                LicenseNumber: request.LicenseNumber);

            var brokerId = await mediator.Send(cmd, ct);
            return Created("", new { brokerId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{brokerId:guid}/score")]
    [Authorize(Policy = "InternalServiceOnly")]
    public async Task<IActionResult> UpdateScore(
        Guid companyId,
        Guid brokerId,
        [FromBody] UpdateScoreRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdateBrokerScoreCommand(brokerId, request.Score), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record AddBrokerRequest(
    string FullName,
    string Email,
    string TempPassword,
    string Phone,
    string LicenseNumber);

public record UpdateScoreRequest(decimal Score);