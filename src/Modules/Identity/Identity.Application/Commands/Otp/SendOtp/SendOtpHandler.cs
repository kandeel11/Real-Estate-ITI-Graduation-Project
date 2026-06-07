// Identity.Application/Features/Auth/Otp/SendOtp/SendOtpHandler.cs

using Identity.Application.Interfaces;
using Identity.Application.Interfaces.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Features.Auth.Otp.SendOtp;

public sealed class SendOtpHandler : IRequestHandler<SendOtpCommand, SendOtpResponse>
{
    private const int OtpLifetimeMinutes = 10;

    private readonly IUserRepository _users;
    private readonly IOtpGenerator _otp;
    private readonly IEmailJobScheduler _emailJobs;   // 🚀 Hangfire wrapper
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendOtpHandler> _logger;

    public SendOtpHandler(
        IUserRepository users,
        IOtpGenerator otp,
        IEmailJobScheduler emailJobs,
        IUnitOfWork uow,
        ILogger<SendOtpHandler> logger)
    {
        _users = users;
        _otp = otp;
        _emailJobs = emailJobs;
        _uow = uow;
        _logger = logger;
    }

    public async Task<SendOtpResponse> Handle(
        SendOtpCommand cmd,
        CancellationToken ct)
    {
        // 1) Find the user
        var user = await _users.GetByEmailAsync(cmd.Email, ct);

        // 🛡️ Don't leak whether the email exists — return success either way
        if (user is null)
        {
            _logger.LogWarning(
                "SendOtp: email not found {Email}. Returning generic success.",
                cmd.Email);

            return new SendOtpResponse(
                Success: true,
                Message: "If an account exists, an OTP has been sent.",
                ExpiresInSeconds: OtpLifetimeMinutes * 60);
        }

        //2) Already verified? skip
        if (user.IsEmailVerified)
        {
            return new SendOtpResponse(
                Success: false,
                Message: "Email is already verified.",
                ExpiresInSeconds: 0);
        }

        // 3) Generate plain code + hash for storage
        var code = _otp.Generate6Digit();
        var codeHash = BCrypt.Net.BCrypt.HashPassword(code);

        user.SetEmailOtp(
            codeHash: codeHash,
            expiresAtUtc: DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes));

        // 4) Commit BEFORE enqueueing — so the job sees the saved state
        await _uow.CommitAsync(ct);

        // 5) Enqueue email — fire & forget, runs in background via Hangfire
        _emailJobs.EnqueueOtp(user.Id, code);

        _logger.LogInformation(
            "OTP issued | UserId={UserId} | ExpiresAt={ExpiresAt}",
            user.Id, user.EmailOtpExpiresAt);

        return new SendOtpResponse(
            Success: true,
            Message: "OTP sent successfully.",
            ExpiresInSeconds: OtpLifetimeMinutes * 60);
    }
}