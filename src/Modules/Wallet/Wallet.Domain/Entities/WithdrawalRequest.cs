using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class WithdrawalRequest
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    /// <summary>Set only for company withdrawals — null for owner withdrawals.</summary>
    public Guid? CompanyId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string Currency { get; private set; } = "AED";
    public string BankAccountDetails { get; private set; } = string.Empty;
    public WithdrawalStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Notes { get; private set; }

    private WithdrawalRequest() { }

    public static WithdrawalRequest Create(Guid ownerId, decimal amount, string bankAccountDetails,
        decimal commissionRate = 0.01m, Guid? companyId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive.", nameof(amount));

        var commission = Math.Round(amount * commissionRate, 2);

        return new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CompanyId = companyId,
            Amount = amount,
            CommissionAmount = commission,
            NetAmount = amount - commission,
            Currency = "AED",
            BankAccountDetails = bankAccountDetails,
            Status = WithdrawalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing()
    {
        if (Status != WithdrawalStatus.Pending)
            throw new InvalidOperationException($"Cannot mark as processing from status {Status}.");
        Status = WithdrawalStatus.Processing;
    }

    public void MarkCompleted()
    {
        if (Status != WithdrawalStatus.Processing)
            throw new InvalidOperationException($"Cannot complete from status {Status}.");
        Status = WithdrawalStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status == WithdrawalStatus.Pending)
            Status = WithdrawalStatus.Processing;
        MarkCompleted();
    }

    public void Reject(string? notes = null)
    {
        if (Status == WithdrawalStatus.Completed)
            throw new InvalidOperationException("Cannot reject an already-completed withdrawal.");
        Status = WithdrawalStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        Notes = notes;
    }
}
