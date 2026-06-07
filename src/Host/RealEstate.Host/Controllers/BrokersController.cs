using Identity.Application.Commands.AddBroker;
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
    [HttpPost]
    public async Task<IActionResult> AddBroker(
        Guid companyId,
        [FromBody] AddBrokerRequest request,
        CancellationToken ct)
    {
        // Company manager can only add brokers to their own company
        if (!currentUser.IsInRole("admin") && currentUser.CompanyId != companyId)
            return Forbid();

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
}

public record AddBrokerRequest(
    string FullName,
    string Email,
    string TempPassword,
    string Phone,
    string LicenseNumber);
