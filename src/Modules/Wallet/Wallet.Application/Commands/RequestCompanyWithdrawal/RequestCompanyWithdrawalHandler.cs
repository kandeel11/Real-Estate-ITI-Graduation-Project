using MediatR;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;

namespace Wallet.Application.Commands.RequestCompanyWithdrawal;

public class RequestCompanyWithdrawalHandler : IRequestHandler<RequestCompanyWithdrawalCommand, Guid>
{
    private readonly IWithdrawalRepository _withdrawalRepo;
    private readonly IWalletRepository _walletRepo;

    public RequestCompanyWithdrawalHandler(IWithdrawalRepository withdrawalRepo, IWalletRepository walletRepo)
    {
        _withdrawalRepo = withdrawalRepo;
        _walletRepo = walletRepo;
    }

    public async Task<Guid> Handle(RequestCompanyWithdrawalCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepo.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        if (wallet is null || wallet.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient company wallet balance.");

        var withdrawal = WithdrawalRequest.Create(
            ownerId: request.CompanyId,
            amount: request.Amount,
            bankAccountDetails: request.BankAccountDetails,
            companyId: request.CompanyId);

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
