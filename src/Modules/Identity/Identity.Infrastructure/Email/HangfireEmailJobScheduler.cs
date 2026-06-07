// Identity.Infrastructure/Email/HangfireEmailJobScheduler.cs

using Hangfire;
using Identity.Application.Interfaces;

namespace Identity.Infrastructure.Email;

public sealed class HangfireEmailJobScheduler : IEmailJobScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireEmailJobScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public string EnqueueOtp(Guid userId, string otpCode) =>
        _jobs.Enqueue<IEmailJobs>(j => j.SendOtpAsync(userId, otpCode));

    public string EnqueueVerificationLink(Guid userId) =>
        _jobs.Enqueue<IEmailJobs>(j => j.SendVerificationLinkAsync(userId));

    public string EnqueueWelcome(Guid userId) =>
        _jobs.Enqueue<IEmailJobs>(j => j.SendWelcomeAsync(userId));

    public string EnqueuePasswordReset(Guid userId, string resetToken) =>
        _jobs.Enqueue<IEmailJobs>(j => j.SendPasswordResetAsync(userId, resetToken));
}