namespace Wallet.Domain.Entities;

public class WalletBalance
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    /// <summary>Set only for company wallets — null for owner wallets.</summary>
    public Guid? CompanyId { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "AED";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private WalletBalance() { }

    public static WalletBalance CreateForOwner(Guid ownerId)
        => new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Balance = 0m,
            Currency = "AED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static WalletBalance CreateForCompany(Guid companyId)
        => new()
        {
            Id = Guid.NewGuid(),
            OwnerId = companyId,
            CompanyId = companyId,
            Balance = 0m,
            Currency = "AED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be positive.", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be positive.", nameof(amount));
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient wallet balance.");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
