// Identity.Application/Interfaces/IEmailVerificationService.cs

using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IEmailVerificationService
{
    Task SendVerificationLinkAsync(User user, CancellationToken ct);
    Task SendOtpAsync(User user, string otpCode, CancellationToken ct);
    Task SendWelcomeEmailAsync(User user, CancellationToken ct);
    Task SendPasswordResetAsync(User user, string resetToken, CancellationToken ct);
}