using Identity.Application.Commands.Login;
using Identity.Application.Commands.Logout;
using Identity.Application.Commands.RefreshToken;
using Identity.Application.Commands.RegisterCompany;
using Identity.Application.Commands.RegisterIndividualUser;
using Identity.Application.Commands.ResendVerificationEmail;
using Identity.Application.Commands.VerifyEmail;
using Identity.Application.Features.Auth.Otp.SendOtp;
using Identity.Application.Features.Auth.Otp.VerifyOtp;
using Identity.Application.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var cmd = new RegisterIndividualUserCommand(
                FullName: request.FullName,
                Email: request.Email,
                Password: request.Password,
                Phone: request.Phone,
                Role: request.Role,
                NationalIdNumber: request.NationalIdNumber);

            var userId = await mediator.Send(cmd, ct);
            return Created("", new { userId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("register-company")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCompany(
        [FromBody] RegisterCompanyCommand cmd, CancellationToken ct)
    {
        try
        {
            var companyId = await mediator.Send(cmd, ct);
            return Created("", new { companyId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var cmd = new LoginCommand(
                request.Email,
                request.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var result = await mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        try
        {
            var cmd = new RefreshTokenCommand(
                request.RefreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var result = await mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token, CancellationToken ct)
    {
        var success = await mediator.Send(new VerifyEmailCommand(token), ct);
        return success
            ? Ok(new { message = "Email verified successfully." })
            : BadRequest(new { error = "Invalid or expired verification token." });
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        await mediator.Send(new ResendVerificationEmailCommand(request.Email), ct);
        return Ok(new { message = "If the email exists and is unverified, a new link has been sent." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetCurrentUserQuery(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("send-otp")]
    [ProducesResponseType(typeof(SendOtpResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Phone,
    string Role,
    string? NationalIdNumber = null);

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record ResendVerificationRequest(string Email);