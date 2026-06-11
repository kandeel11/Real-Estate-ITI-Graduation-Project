using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.ConfirmPayment;

public record ConfirmPaymentCommand(Guid PaymentId) : IRequest;

public class ConfirmPaymentHandler(IMaintenancePaymentRepository paymentRepo, IMaintenanceTicketRepository ticketRepo)
    : IRequestHandler<ConfirmPaymentCommand>
{
    public async Task Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {cmd.PaymentId} not found.");
        payment.Confirm();
        var ticket = await ticketRepo.GetByIdAsync(payment.TicketId, ct)
            ?? throw new KeyNotFoundException("Ticket not found.");
        ticket.ConfirmPayment();
        await paymentRepo.SaveChangesAsync(ct);
    }
}