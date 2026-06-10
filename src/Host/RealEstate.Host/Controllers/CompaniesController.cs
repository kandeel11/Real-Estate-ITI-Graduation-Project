using Identity.Application.Commands.UpdateCompany;
using Identity.Application.Commands.VerifyCompany;
using Identity.Application.Interfaces;
using Identity.Application.Queries.GetBrokersByCompany;
using Identity.Application.Queries.GetCompanyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertiesByCompany;
using RealEstate.Host.Services;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController(IMediator mediator, ICurrentUserService currentUser, PropertyEnrichmentService enrichment) : ControllerBase
{
    /// <summary>
    /// Returns the company belonging to the logged-in company manager (reads CompanyId from JWT).
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var company = await mediator.Send(new GetCompanyByIdQuery(companyId.Value), ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>
    /// Returns a company by ID. Accessible to admins or the company's own manager.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var company = await mediator.Send(new GetCompanyByIdQuery(id), ct);
        return company is null ? NotFound() : Ok(company);
    }

    /// <summary>
    /// Updates the logged-in manager's OWN company profile (companyId from JWT).
    /// </summary>
    [HttpPut("mine")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> UpdateMine([FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        var companyId = currentUser.CompanyId;
        if (companyId is null) return BadRequest(new { error = "No company is associated with your account." });

        try
        {
            await mediator.Send(new UpdateCompanyCommand(
                CompanyId: companyId.Value,
                RequestingUserId: userId.Value,
                Name: request.Name,
                City: request.City,
                Address: request.Address,
                Phone: request.Phone,
                Website: request.Website,
                Description: request.Description,
                LogoUrl: request.LogoUrl), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    /// <summary>
    /// Updates company profile. Only the company's manager or an admin can call this.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        // Verify caller manages this company (unless admin)
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != id)
            return Forbid();

        try
        {
            await mediator.Send(new UpdateCompanyCommand(
                CompanyId: id,
                RequestingUserId: userId.Value,
                Name: request.Name,
                City: request.City,
                Address: request.Address,
                Phone: request.Phone,
                Website: request.Website,
                Description: request.Description,
                LogoUrl: request.LogoUrl), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Marks a company as verified. Admin only.
    /// </summary>
    [HttpPut("{id:guid}/verify")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new VerifyCompanyCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lists all properties belonging to the logged-in manager's OWN company (companyId from JWT).
    /// </summary>
    [HttpGet("mine/properties")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyProperties(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(companyId.Value), ct);
        await enrichment.EnrichAsync(properties, ct);
        return Ok(properties);
    }

    /// <summary>
    /// Lists all brokers belonging to the logged-in manager's OWN company (companyId from JWT).
    /// </summary>
    [HttpGet("mine/brokers")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetMyBrokers(CancellationToken ct)
    {
        var companyId = currentUser.CompanyId;
        if (companyId is null)
            return BadRequest(new { error = "No company is associated with your account." });

        var brokers = await mediator.Send(new GetBrokersByCompanyQuery(companyId.Value), ct);
        return Ok(brokers);
    }

    /// <summary>
    /// Lists all properties belonging to a company (created by any of its brokers or the manager).
    /// </summary>
    [HttpGet("{id:guid}/properties")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProperties(Guid id, CancellationToken ct)
    {
        var properties = await mediator.Send(new GetPropertiesByCompanyQuery(id), ct);
        await enrichment.EnrichAsync(properties, ct);
        return Ok(properties);
    }

    /// <summary>
    /// Lists all brokers belonging to a company.
    /// </summary>
    [HttpGet("{id:guid}/brokers")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> GetBrokers(Guid id, CancellationToken ct)
    {
        // Company manager can only see their own company's brokers
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != id)
            return Forbid();

        var brokers = await mediator.Send(new GetBrokersByCompanyQuery(id), ct);
        return Ok(brokers);
    }
}

public record UpdateCompanyRequest(
    string Name,
    string City,
    string Address,
    string Phone,
    string? Website,
    string? Description,
    string? LogoUrl);
