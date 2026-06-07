using Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

public class User : BaseEntity
{
    public Guid? CompanyId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? NationalIdNumber { get; private set; }
    public string? ProfileImageUrl { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? BannedAt { get; private set; }

    private User() { }

    public static User Create(
        string fullName,
        string email,
        string passwordHash,
        string phone,
        UserRole role,
        string? nationalIdNumber = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required");

        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Phone = phone,
            Role = role.ToRoleString(),
            NationalIdNumber = nationalIdNumber,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationToken = Guid.NewGuid().ToString("N"),
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.Role));
        return user;
    }

    public void VerifyEmail(string token)
    {
        if (IsEmailVerified) return;
        if (EmailVerificationToken != token)
            throw new InvalidOperationException("Invalid verification token");
        if (EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Verification token has expired");

        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAt = null;
        AddDomainEvent(new EmailVerifiedEvent(Id));
    }

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void SetCompany(Guid companyId) => CompanyId = companyId;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Ban(string reason)
    {
        IsActive = false;
        BanReason = reason;
        BannedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string phone, string? imageUrl)
    {
        FullName = fullName.Trim();
        Phone = phone;
        ProfileImageUrl = imageUrl;
        MarkAsUpdated();
    }

    public void RegenerateVerificationToken()
    {
        EmailVerificationToken = Guid.NewGuid().ToString("N");
        EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        MarkAsUpdated();
    }
}
