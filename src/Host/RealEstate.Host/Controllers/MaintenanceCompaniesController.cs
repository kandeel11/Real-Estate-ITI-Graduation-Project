using Identity.Application.Commands.RegisterMaintenanceCompany;
using Identity.Application.Interfaces;
using MaintenanceCompanies.Application.Commands.CreateCompany;
using MaintenanceCompanies.Application.Commands.UpdateCompanyProfile;
using MaintenanceCompanies.Application.DTOs.Companies;
using MaintenanceCompanies.Application.Queries.GetCompanyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance-companies")]
[Authorize(Policy = "AdminOnly")]
public class MaintenanceCompaniesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceCompanyRequest request,
        CancellationToken ct)
    {
        try
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SystemAdmin";

            // 1. Identity Command (Create User Account)
            var userId = await mediator.Send(new RegisterMaintenanceCompanyCommand(
                FullName: request.FullName,
                Email: request.Email,
                Phone: request.Phone,
                TempPassword: request.TempPassword
            ), ct);

            // 2. Maintenance Command (Create Company Profile)
            var companyProfile = await mediator.Send(new CreateMaintenanceCompanyCommand(
                UserId: userId.ToString(),
                AdminId: adminId,
                CompanyName: request.CompanyName,
                Email: request.Email,
                Phone: request.Phone,
                City: request.City,
                ServiceRadiusKm: request.ServiceRadiusKm ?? 25,
                Description: request.Description,
                LogoUrl: request.LogoUrl,
                CategoryIds: request.CategoryIds
            ), ct);

            return Created("", new
            {
                CompanyProfile = companyProfile,
                Message = "the company profile was created successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize(Policy = "MaintenanceCompany")]
    public async Task<IActionResult> GetMyProfile(
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct)
    {
        var userId = currentUser.UserId?.ToString();
        if (userId == null) return Unauthorized();

        var profile = await mediator.Send(new GetCompanyProfileQuery(userId), ct);

        if (profile == null)
            return NotFound(new { message = "Company profile not found." });

        return Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize(Policy = "MaintenanceCompany")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMaintenanceCompanyDto request,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken ct)
    {
        var userId = currentUser.UserId?.ToString();
        if (userId == null) return Unauthorized();

        try
        {
            var updatedProfile = await mediator.Send(new UpdateCompanyProfileCommand(
                UserId: userId,
                CompanyName: request.CompanyName,
                Phone: request.Phone,
                City: request.City,
                ServiceRadiusKm: request.ServiceRadiusKm,
                Description: request.Description,
                LogoUrl: request.LogoUrl,
                CategoryIds: request.CategoryIds
            ), ct);

            return Ok(new
            {
                Profile = updatedProfile,
                Message = "Company profile updated successfully."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request record
public record CreateMaintenanceCompanyRequest(
    string CompanyName,
    List<Guid> CategoryIds,
    string Email,
    string Phone,
    string City,
    string FullName,
    string TempPassword,
    int? ServiceRadiusKm,
    string? Description,
    string? LogoUrl
);