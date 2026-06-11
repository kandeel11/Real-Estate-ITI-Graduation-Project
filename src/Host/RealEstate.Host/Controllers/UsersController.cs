using Identity.Application.Commands.ChangePassword;
using Identity.Application.Commands.UpdateProfile;
using Identity.Application.Interfaces;
using Identity.Application.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

/// <summary>
/// Role-agnostic self-service profile endpoints. TenantController has
/// tenant-locked equivalents; these serve every authenticated role
/// (admin/company manager/broker/owner) reusing the same Identity commands.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var profile = await mediator.Send(new GetMyProfileQuery(UserId), ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileRequest req, CancellationToken ct)
    {
        await mediator.Send(new UpdateProfileCommand(UserId, req.FullName, req.Phone, req.ProfileImageUrl), ct);
        return NoContent();
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeMyPasswordRequest req, CancellationToken ct)
    {
        if (req.NewPassword != req.ConfirmNewPassword)
            return BadRequest(new { error = "New password and confirmation do not match." });
        try
        {
            await mediator.Send(new ChangePasswordCommand(UserId, req.CurrentPassword, req.NewPassword), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record UpdateMyProfileRequest(string FullName, string Phone, string? ProfileImageUrl);
public record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
