// Identity.Infrastructure/Email/IEmailJobs.cs

namespace Identity.Infrastructure.Email;

public interface IEmailJobs
{
    Task SendOtpAsync(Guid userId, string otpCode);
    Task SendVerificationLinkAsync(Guid userId);
    Task SendWelcomeAsync(Guid userId);
    Task SendPasswordResetAsync(Guid userId, string resetToken);
}