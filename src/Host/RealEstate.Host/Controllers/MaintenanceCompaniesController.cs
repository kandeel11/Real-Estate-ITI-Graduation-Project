using Identity.Application.Commands.RegisterMaintenanceCompany;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance-companies")]
[Authorize(Policy = "AdminOnly")]
public class MaintenanceCompaniesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Admin creates a maintenance company account. Auto-verified — no separate verification step.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceCompanyRequest request,
        CancellationToken ct)
    {
        try
        {
            var companyId = await mediator.Send(new RegisterMaintenanceCompanyCommand(
                CompanyName: request.CompanyName,
                ServiceTypes: request.ServiceTypes,
                Email: request.Email,
                Phone: request.Phone,
                City: request.City,
                FullName: request.FullName,
                TempPassword: request.TempPassword), ct);

            return Created("", new
            {
                companyId,
                message = "Maintenance company created. The account is active and verified. Share the email and temp password with the company."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record CreateMaintenanceCompanyRequest(
    string CompanyName,
    string[] ServiceTypes,
    string Email,
    string Phone,
    string City,
    string FullName,
    string TempPassword);
