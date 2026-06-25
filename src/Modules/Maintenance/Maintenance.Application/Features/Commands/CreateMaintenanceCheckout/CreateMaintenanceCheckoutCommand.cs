using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.CreateMaintenanceCheckout;

public record CreateMaintenanceCheckoutCommand(Guid TicketId) : IRequest<string>;

public class CreateMaintenanceCheckoutHandler(
    IMaintenancePaymentRepository paymentRepo,
    IMaintenancePaymentLinkService paymentLinkService)
    : IRequestHandler<CreateMaintenanceCheckoutCommand, string>
{
    public async Task<string> Handle(CreateMaintenanceCheckoutCommand cmd, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByTicketIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Payment not found for ticket {cmd.TicketId}.");

        if (payment.CheckoutUrl is not null)
            return payment.CheckoutUrl;

        var (checkoutUrl, sessionId) = await paymentLinkService.CreateAsync(
            payment.Id, payment.Amount, payment.Currency, ct);

        payment.SetCheckoutSession(sessionId, checkoutUrl);
        await paymentRepo.SaveChangesAsync(ct);

        return checkoutUrl;
    }
}
