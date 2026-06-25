using MediatR;
using Maintenance.Application.Interfaces;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Maintenance.Application.Features.EventHandlers;

public class HandleMaintenancePaymentConfirmedHandler(
    IMaintenancePaymentRepository paymentRepo,
    IMaintenanceTicketRepository ticketRepo)
    : INotificationHandler<MaintenancePaymentConfirmedEvent>
{
    public async Task Handle(MaintenancePaymentConfirmedEvent notification, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByIdAsync(notification.MaintenancePaymentId, ct);
        if (payment is null)
        {
            Console.Error.WriteLine(
                $"[MaintenancePaymentConfirmed] Payment {notification.MaintenancePaymentId} not found.");
            return;
        }

        var confirmed = payment.TryConfirmViaWebhook(notification.StripeTransactionId);
        if (!confirmed)
            return; // already Paid — idempotent

        var ticket = await ticketRepo.GetByIdAsync(payment.TicketId, ct);
        if (ticket is not null)
            ticket.ConfirmPayment();

        await paymentRepo.SaveChangesAsync(ct);
    }
}
