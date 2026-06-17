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

            var entry = LedgerEntry.CreateCredit(
                walletId: wallet.Id,
                paymentId: notification.PaymentId,
                amount: notification.Amount,
                description: $"Rent payment for lease {notification.LeaseId}");

            wallet.Credit(notification.Amount);

            await _walletRepo.AddLedgerEntryAsync(entry, cancellationToken);
            await _walletRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Wallet credited: OwnerId={OwnerId}, Amount={Amount}, PaymentId={PaymentId}",
                notification.OwnerId, notification.Amount, notification.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RentPaymentSucceededHandler: Failed for PaymentId={PaymentId}", notification.PaymentId);
            // Don't rethrow — failures must not crash the outbox publisher
        }
    }
}
