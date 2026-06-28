using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using Wallet.Application.Interfaces;
using Wallet.Domain.Entities;

namespace Wallet.Application.EventHandlers;

public class RentPaymentSucceededHandler : INotificationHandler<RentPaymentSucceededEvent>
{
    private readonly IWalletRepository _walletRepo;
    private readonly ILogger<RentPaymentSucceededHandler> _logger;

    public RentPaymentSucceededHandler(IWalletRepository walletRepo, ILogger<RentPaymentSucceededHandler> logger)
    {
        _walletRepo = walletRepo;
        _logger = logger;
    }

    public async Task Handle(RentPaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        // Company-managed deals: the entire net rent goes to the company wallet (handled by CompanyWalletRentCreditHandler)
        if (notification.CompanyId is not null)
            return;

        try
        {
            // Idempotency: skip if this payment was already credited
            if (await _walletRepo.LedgerEntryExistsAsync(notification.PaymentId, cancellationToken))
            {
                _logger.LogInformation(
                    "RentPaymentSucceededHandler: PaymentId={PaymentId} already credited. Skipping.",
                    notification.PaymentId);
                return;
            }

            var wallet = await _walletRepo.GetOrCreateAsync(notification.OwnerId, cancellationToken);

            // 1% platform commission — landlord receives 99% of the rent payment
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
                "Owner wallet credited: OwnerId={OwnerId}, NetAmount={Amount}, PaymentId={PaymentId}",
                notification.OwnerId, netAmount, notification.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RentPaymentSucceededHandler: Failed for PaymentId={PaymentId}", notification.PaymentId);
        }
    }
}
