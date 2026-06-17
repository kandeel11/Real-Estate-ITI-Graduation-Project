using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Guid PaymentId { get; private set; }
    public LedgerEntryType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry CreateCredit(Guid walletId, Guid paymentId, decimal amount, string description)
        => new()
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            PaymentId = paymentId,
            Type = LedgerEntryType.Credit,
            Amount = amount,
            Description = description,
            OccurredAt = DateTime.UtcNow
        };

    public static LedgerEntry CreateDebit(Guid walletId, Guid paymentId, decimal amount, string description)
        => new()
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            PaymentId = paymentId,
            Type = LedgerEntryType.Debit,
            Amount = amount,
            Description = description,
            OccurredAt = DateTime.UtcNow
        };
}
