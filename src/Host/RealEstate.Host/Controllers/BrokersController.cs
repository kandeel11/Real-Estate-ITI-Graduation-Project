using Identity.Application.Commands.AddBroker;
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
    [HttpPost]
    public async Task<IActionResult> AddBroker(
        Guid companyId,
        [FromBody] AddBrokerRequest request,
        CancellationToken ct)
    {
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