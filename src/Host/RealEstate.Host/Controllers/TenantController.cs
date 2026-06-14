using Document.Application.Queries.Lease;
using Identity.Application.Commands.ChangePassword;
using Identity.Application.Commands.UpdateProfile;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertyById;
using RealEstate.Host.Services;
using Identity.Application.Queries.GetCurrentUser;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize(Roles = "tenant")]
public class TenantController(
    ISender sender,
    ICurrentUserService currentUser,
    PropertyEnrichmentService enrichment) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var user = await sender.Send(new GetCurrentUserQuery(), ct);
        if (user is null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            phone = user.Phone,
            role = user.Role,
            isEmailVerified = user.IsEmailVerified,
            isActive = user.IsActive
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        await sender.Send(new UpdateProfileCommand(UserId, req.FullName, req.Phone, req.Email), ct);
        return NoContent();
    }

    [HttpPut("profile/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest(new { error = "New password and confirmation do not match." });
        try
        {
            await sender.Send(new ChangePasswordCommand(UserId, req.CurrentPassword, req.NewPassword), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("rented-properties")]
    public async Task<IActionResult> GetRentedProperties(CancellationToken ct)
    {
        var leases = await sender.Send(new GetMyLeasesQuery(UserId), ct);
        var propertyIds = leases.Select(l => l.PropertyId).Distinct().ToList();
        var result = new List<object>();

        foreach (var pid in propertyIds)
        {
            var prop = await sender.Send(new GetPropertyByIdQuery(pid), ct);
            if (prop is null) continue;
            await enrichment.EnrichAsync(prop, ct);

            var lease = leases
                .Where(l => l.PropertyId == pid)
                .OrderByDescending(l => l.IsActive)
                .ThenByDescending(l => l.StartDate)
                .First();

            var daysLeft = lease.EndDate.HasValue
                ? (int)(lease.EndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).TotalDays
                : (int?)null;

            result.Add(new
            {
                property = prop,
                leaseId = lease.Id,
                monthlyRent = lease.RentAmount,
                startDate = lease.StartDate,
                endDate = lease.EndDate,
                isActive = lease.IsActive,
                daysUntilExpiry = daysLeft
            });
        }

        return Ok(result);
    }

    [HttpGet("leases")]
    public async Task<IActionResult> GetLeases(CancellationToken ct)
        => Ok(await sender.Send(new GetMyLeasesQuery(UserId), ct));

    [HttpGet("leases/active")]
    public async Task<IActionResult> GetActiveLease(CancellationToken ct)
    {
        var lease = await sender.Send(new GetActiveLeaseQuery(UserId), ct);
        return lease is null ? NotFound(new { message = "No active lease found." }) : Ok(lease);
    }
}

public record UpdateProfileRequest(string FullName, string Phone, string? Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);