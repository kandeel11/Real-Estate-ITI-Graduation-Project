using MediatR;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;

namespace Wallet.Application.Commands.RequestWithdrawal;

public class RequestWithdrawalHandler : IRequestHandler<RequestWithdrawalCommand, Guid>
{
    private readonly IWithdrawalRepository _withdrawalRepo;
    private readonly IWalletRepository _walletRepo;

    public RequestWithdrawalHandler(IWithdrawalRepository withdrawalRepo, IWalletRepository walletRepo)
    {
        _withdrawalRepo = withdrawalRepo;
        _walletRepo = walletRepo;
    }

    public async Task<Guid> Handle(RequestWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepo.GetByOwnerIdAsync(request.OwnerId, cancellationToken);

        if (wallet is null || wallet.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient wallet balance.");

        var withdrawal = WithdrawalRequest.Create(
            request.OwnerId,
            request.Amount,
            request.BankAccountDetails);

        // Debit the full requested amount — commission is already embedded in NetAmount
        wallet.Debit(request.Amount);

        var debitEntry = LedgerEntry.CreateDebit(
            walletId: wallet.Id,
            paymentId: withdrawal.Id,
            amount: request.Amount,
            description: $"Withdrawal request — net payout {withdrawal.NetAmount:0.##} AED after {withdrawal.CommissionAmount:0.##} AED platform commission");

        await _walletRepo.AddLedgerEntryAsync(debitEntry, cancellationToken);
        await _walletRepo.SaveChangesAsync(cancellationToken);

        return await _withdrawalRepo.CreateAsync(withdrawal, cancellationToken);
    }
}
