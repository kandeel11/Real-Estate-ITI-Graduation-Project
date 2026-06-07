using Domain.Entities;

namespace Identity.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? CreatedByIp { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string token,
        DateTime expiresAt,
        string? createdByIp = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp
        };
    }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public void Revoke(string? replacedBy = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedBy;
    }
}
