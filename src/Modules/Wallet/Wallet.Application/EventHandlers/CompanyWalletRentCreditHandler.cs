using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;

namespace Wallet.Application.EventHandlers;

/// <summary>
/// Credits the company wallet with a management fee when a rent payment succeeds
/// on a company-managed lease. Only fires when CompanyId is present on the event.
/// </summary>
public class CompanyWalletRentCreditHandler : INotificationHandler<RentPaymentSucceededEvent>
{
    private readonly IWalletRepository _walletRepo;
    private readonly ILogger<CompanyWalletRentCreditHandler> _logger;

    public CompanyWalletRentCreditHandler(IWalletRepository walletRepo, ILogger<CompanyWalletRentCreditHandler> logger)
    {
        _walletRepo = walletRepo;
        _logger = logger;
    }

    public async Task Handle(RentPaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        if (notification.CompanyId is null)
            return;

        var companyId = notification.CompanyId.Value;

        try
        {
            // Idempotency: skip if already credited to any company wallet for this payment
            if (await _walletRepo.CompanyLedgerEntryExistsAsync(notification.PaymentId, cancellationToken))
            {
                _logger.LogInformation(
                    "CompanyWalletRentCreditHandler: PaymentId={PaymentId} already credited to company wallet. Skipping.",
                    notification.PaymentId);
                return;
            }

            var wallet = await _walletRepo.GetOrCreateForCompanyAsync(companyId, cancellationToken);

            // Company receives full rent minus 1% platform commission
            var netAmount = Math.Round(notification.Amount * 0.99m, 2);
            var commission = notification.Amount - netAmount;

            var propertyLabel = notification.PropertyTitle is not null
                ? $"property \"{notification.PropertyTitle}\""
                : $"property {notification.PropertyId}";

            var dueLabel = notification.DueDate is not null
                ? $" (due {notification.DueDate:MMM yyyy})"
                : string.Empty;

            var entry = LedgerEntry.CreateCredit(
                walletId: wallet.Id,
                paymentId: notification.PaymentId,
                amount: netAmount,
                description: $"Rent payment — {propertyLabel}{dueLabel}, lease {notification.LeaseId} (net of {commission:0.##} AED commission)");

            wallet.Credit(netAmount);

            await _walletRepo.AddLedgerEntryAsync(entry, cancellationToken);
            await _walletRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Company wallet credited: CompanyId={CompanyId}, NetAmount={Amount}, PaymentId={PaymentId}",
                companyId, netAmount, notification.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CompanyWalletRentCreditHandler: Failed for PaymentId={PaymentId}", notification.PaymentId);
        }
    }
}
