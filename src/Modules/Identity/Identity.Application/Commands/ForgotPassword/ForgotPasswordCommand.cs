using Identity.Application.Interfaces;
using Identity.Application.Interfaces.Common;
using MediatR;

namespace Identity.Application.Commands.ForgotPassword;

/// <summary>
/// Sends a password-reset OTP to the email (reuses the email-OTP fields on
/// the User entity — no new columns/migration). Always reports success so
/// account existence isn't leaked.
/// </summary>
public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;

public record ForgotPasswordResponse(bool Success, string Message, int ExpiresInSeconds);

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private const int OtpLifetimeMinutes = 10;

    private readonly IUserRepository _users;
    private readonly IOtpGenerator _otp;
    private readonly IEmailJobScheduler _emailJobs;
    private readonly IUnitOfWork _uow;

    public ForgotPasswordHandler(
        IUserRepository users,
        IOtpGenerator otp,
        IEmailJobScheduler emailJobs,
        IUnitOfWork uow)
    {
        _users = users;
        _otp = otp;
        _emailJobs = emailJobs;
        _uow = uow;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(cmd.Email, ct);

        // Don't leak whether the email exists
        if (user is null)
            return new ForgotPasswordResponse(true, "If an account exists, a reset code has been sent.", OtpLifetimeMinutes * 60);

        var code = _otp.Generate6Digit();
        var codeHash = BCrypt.Net.BCrypt.HashPassword(code);

        // SetPasswordResetOtp — unlike SetEmailOtp it works for VERIFIED
        // accounts (which is the normal case for a password reset)
        user.SetPasswordResetOtp(codeHash, DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes));
        await _uow.CommitAsync(ct);

        _emailJobs.EnqueueOtp(user.Id, code);

        return new ForgotPasswordResponse(true, "If an account exists, a reset code has been sent.", OtpLifetimeMinutes * 60);
    }
}
