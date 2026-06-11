// Identity.Domain/Entities/User.cs

using Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

public class User : BaseEntity
{
    // ── Existing fields ──────────────────────────────────────────────────
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

    // ── Email verification (link-based) ──────────────────────────────────
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }

    // ── 🆕 Email OTP (code-based) ────────────────────────────────────────
    public string? EmailOtpHash { get; private set; }
    public DateTime? EmailOtpExpiresAt { get; private set; }
    public int EmailOtpAttempts { get; private set; }   // brute-force guard
    public DateTime? LastOtpRequestedAt { get; private set; }   // resend-cooldown guard

    // ── Activity / moderation ────────────────────────────────────────────
    public DateTime? LastLoginAt { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? BannedAt { get; private set; }

    // ── Constants ────────────────────────────────────────────────────────
    private const int OtpLifetimeMinutes = 10;
    private const int OtpMaxAttempts = 5;
    private const int OtpResendCooldownSeconds = 60;

    // ── EF Core constructor ──────────────────────────────────────────────
    private User() { }

    // ── Factory ──────────────────────────────────────────────────────────
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

    // ── Email verification (LINK-based) ──────────────────────────────────
    public void VerifyEmail(string token)
    {
        if (IsEmailVerified) return;

        if (EmailVerificationToken != token)
            throw new InvalidOperationException("Invalid verification token");

        if (EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Verification token has expired");

        MarkEmailVerified();
    }

    public void RegenerateVerificationToken()
    {
        EmailVerificationToken = Guid.NewGuid().ToString("N");
        EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        MarkAsUpdated();
    }

    // ────────────────────────────────────────────────────────────────────
    // 🆕 Email verification (OTP-based)
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Stores a new OTP hash and resets attempt counter.
    /// Enforces a resend cooldown to prevent abuse.
    /// </summary>
    public void SetEmailOtp(string codeHash, DateTime expiresAtUtc)
    {
        if (IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        if (LastOtpRequestedAt is not null &&
            (DateTime.UtcNow - LastOtpRequestedAt.Value).TotalSeconds < OtpResendCooldownSeconds)
        {
            var wait = OtpResendCooldownSeconds -
                       (int)(DateTime.UtcNow - LastOtpRequestedAt.Value).TotalSeconds;
            throw new InvalidOperationException(
                $"Please wait {wait} seconds before requesting another OTP.");
        }

        EmailOtpHash = codeHash;
        EmailOtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpLifetimeMinutes);
        EmailOtpAttempts = 0;
        LastOtpRequestedAt = DateTime.UtcNow;

        MarkAsUpdated();
        AddDomainEvent(new OtpRequestedEvent(Id, Email, EmailOtpExpiresAt.Value));
    }

    /// <summary>
    /// Verifies a plain-text OTP against the stored hash.
    /// Updates attempts, expiry, and verification state in-place.
    /// </summary>
    public OtpVerificationResult VerifyEmailOtp(string code)
    {
        if (IsEmailVerified)
            return OtpVerificationResult.AlreadyVerified;

        if (EmailOtpHash is null || EmailOtpExpiresAt is null)
            return OtpVerificationResult.NotIssued;

        if (EmailOtpExpiresAt < DateTime.UtcNow)
            return OtpVerificationResult.Expired;

        if (EmailOtpAttempts >= OtpMaxAttempts)
        {
            ClearOtp();                 // invalidate after too many tries
            MarkAsUpdated();
            return OtpVerificationResult.TooManyAttempts;
        }

        var ok = BCrypt.Net.BCrypt.Verify(code, EmailOtpHash);

        if (!ok)
        {
            EmailOtpAttempts++;
            MarkAsUpdated();
            return OtpVerificationResult.Invalid;
        }

        // ✅ success
        MarkEmailVerified();
        ClearOtp();
        MarkAsUpdated();
        return OtpVerificationResult.Success;
    }

    // ── Activity ─────────────────────────────────────────────────────────
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

    //public void UpdateProfile(string fullName, string phone, string? imageUrl)
    //{
    //    FullName = fullName.Trim();
    //    Phone = phone;
    //    ProfileImageUrl = imageUrl;
    //    MarkAsUpdated();
    //}
    public void UpdateProfile(string fullName, string phone, string? email)
    {
        FullName = fullName;
        Phone = phone;
        if (!string.IsNullOrWhiteSpace(email))
            Email = email;
        MarkAsUpdated();
    }

    // ── Private helpers ──────────────────────────────────────────────────
    private void MarkEmailVerified()
    {
        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAt = null;

        AddDomainEvent(new EmailVerifiedEvent(Id));
    }

    private void ClearOtp()
    {
        EmailOtpHash = null;
        EmailOtpExpiresAt = null;
        EmailOtpAttempts = 0;
    }
}

// ── OTP result enum ──────────────────────────────────────────────────────
public enum OtpVerificationResult
{
    Success,
    Invalid,
    Expired,
    NotIssued,
    TooManyAttempts,
    AlreadyVerified
}