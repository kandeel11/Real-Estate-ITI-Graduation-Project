// Identity.Application/Interfaces/IEmailJobScheduler.cs

namespace Identity.Application.Interfaces;

public interface IEmailJobScheduler
{
    string EnqueueOtp(Guid userId, string otpCode);
    string EnqueueVerificationLink(Guid userId);
    string EnqueueWelcome(Guid userId);
    string EnqueuePasswordReset(Guid userId, string resetToken);
}