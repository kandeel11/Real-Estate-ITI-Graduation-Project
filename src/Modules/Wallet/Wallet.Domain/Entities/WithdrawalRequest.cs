using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class WithdrawalRequest
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "AED";
    public string BankAccountDetails { get; private set; } = string.Empty;
    public WithdrawalStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Notes { get; private set; }

    private WithdrawalRequest() { }

    public static WithdrawalRequest Create(Guid ownerId, decimal amount, string bankAccountDetails)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive.", nameof(amount));

        return new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Amount = amount,
            Currency = "AED",
            BankAccountDetails = bankAccountDetails,
            Status = WithdrawalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }
}
