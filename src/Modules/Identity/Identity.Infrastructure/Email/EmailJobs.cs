// Identity.Infrastructure/Email/EmailJobs.cs

using Hangfire;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Email;

public sealed class EmailJobs : IEmailJobs
{
    private readonly IUserRepository _users;
    private readonly IEmailVerificationService _email;
    private readonly ILogger<EmailJobs> _logger;

    public EmailJobs(
        IUserRepository users,
        IEmailVerificationService email,
        ILogger<EmailJobs> logger)
    {
        _users = users;
        _email = email;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    [Queue("emails")]
    public async Task SendOtpAsync(Guid userId, string otpCode)
    {
        var user = await _users.GetByIdAsync(userId, CancellationToken.None);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found — skipping OTP email.", userId);
            return;
        }

        // ⚠️ مهم: نستخدم CancellationToken.None هنا لأنها Background Job
        await _email.SendOtpAsync(user, otpCode, CancellationToken.None);
    }

    [AutomaticRetry(Attempts = 3)]
    [Queue("emails")]
    public async Task SendVerificationLinkAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId, CancellationToken.None);
        if (user is null) return;
        await _email.SendVerificationLinkAsync(user, CancellationToken.None);
    }

    [AutomaticRetry(Attempts = 3)]
    [Queue("emails")]
    public async Task SendWelcomeAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId, CancellationToken.None);
        if (user is null) return;
        await _email.SendWelcomeEmailAsync(user, CancellationToken.None);
    }

    [AutomaticRetry(Attempts = 3)]
    [Queue("emails")]
    public async Task SendPasswordResetAsync(Guid userId, string resetToken)
    {
        var user = await _users.GetByIdAsync(userId, CancellationToken.None);
        if (user is null) return;
        await _email.SendPasswordResetAsync(user, resetToken, CancellationToken.None);
    }
}