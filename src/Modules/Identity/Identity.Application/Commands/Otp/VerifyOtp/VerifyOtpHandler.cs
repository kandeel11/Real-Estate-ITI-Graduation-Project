// Identity.Application/Features/Auth/Otp/VerifyOtp/VerifyOtpHandler.cs

using Identity.Application.Interfaces;
using Identity.Application.Interfaces.Common;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Auth.Otp.VerifyOtp;

public sealed class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResponse>
{
    private readonly IUserRepository _users;
    private readonly IEmailJobScheduler _emailJobs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<VerifyOtpHandler> _logger;

    public VerifyOtpHandler(
        IUserRepository users,
        IEmailJobScheduler emailJobs,
        IUnitOfWork uow,
        ILogger<VerifyOtpHandler> logger)
    {
        _users = users;
        _emailJobs = emailJobs;
        _uow = uow;
        _logger = logger;
    }

    public async Task<VerifyOtpResponse> Handle(
        VerifyOtpCommand cmd,
        CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(cmd.Email, ct);
        if (user is null)
            return new VerifyOtpResponse(false, "Invalid email or code.");

        var result = user.VerifyEmailOtp(cmd.Code);

        // 💾 Persist — we always save, because attempts/state changed
        await _uow.CommitAsync(ct);

        _logger.LogInformation(
            "OTP verification | UserId={UserId} | Result={Result}",
            user.Id, result);

        return result switch
        {
            OtpVerificationResult.Success =>
                OnSuccess(user),

            OtpVerificationResult.Invalid =>
                new(false, "Invalid OTP code."),

            OtpVerificationResult.Expired =>
                new(false, "OTP has expired. Please request a new one."),

            OtpVerificationResult.NotIssued =>
                new(false, "No OTP has been issued. Please request one."),

            OtpVerificationResult.TooManyAttempts =>
                new(false, "Too many incorrect attempts. Please request a new OTP."),

            _ => new VerifyOtpResponse(false, "OTP verification failed.")
        };
    }

    private VerifyOtpResponse OnSuccess(User user)
    {
        // 🎉 Fire welcome email after successful verification
        _emailJobs.EnqueueWelcome(user.Id);

        return new VerifyOtpResponse(true, "Email verified successfully.");
    }
}