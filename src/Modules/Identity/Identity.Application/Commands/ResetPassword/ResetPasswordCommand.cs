using Identity.Application.Interfaces;
using Identity.Application.Interfaces.Common;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands.ResetPassword;

/// <summary>
/// Verifies the password-reset OTP (sent by ForgotPasswordCommand) and sets
/// the new password.
/// </summary>
public record ResetPasswordCommand(string Email, string Code, string NewPassword)
    : IRequest<ResetPasswordResponse>;

public record ResetPasswordResponse(bool Success, string Message);

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;

    public ResetPasswordHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork uow)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(cmd.Email, ct);
        if (user is null)
            return new ResetPasswordResponse(false, "Invalid email or code.");

        // VerifyPasswordResetOtp — works regardless of verification state
        var result = user.VerifyPasswordResetOtp(cmd.Code);

        if (result == OtpVerificationResult.Success)
        {
            user.SetPasswordHash(_hasher.Hash(cmd.NewPassword));
        }

        // Always persist — attempt counters/state changed either way
        await _uow.CommitAsync(ct);

        return result switch
        {
            OtpVerificationResult.Success => new(true, "Password has been reset. You can sign in now."),
            OtpVerificationResult.Invalid => new(false, "Invalid reset code."),
            OtpVerificationResult.Expired => new(false, "Reset code has expired. Please request a new one."),
            OtpVerificationResult.NotIssued => new(false, "No reset code has been requested."),
            OtpVerificationResult.TooManyAttempts => new(false, "Too many incorrect attempts. Please request a new code."),
            _ => new ResetPasswordResponse(false, "Password reset failed."),
        };
    }
}
