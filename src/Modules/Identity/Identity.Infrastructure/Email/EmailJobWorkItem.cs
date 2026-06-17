namespace Identity.Infrastructure.Email;

public enum EmailJobType
{
    Otp,
    VerificationLink,
    Welcome,
    PasswordReset
}

public sealed record EmailJobWorkItem(
    EmailJobType Type,
    Guid UserId,
    string? OtpCode = null,
    string? ResetToken = null
);
