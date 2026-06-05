using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid TicketId, Guid PayerId, Guid VendorId,
    decimal Amount, string Currency = "SAR",
    string? PaymentMethod = null) : IRequest<Guid>;

public class CreatePaymentHandler(IMaintenanceTicketRepository ticketRepo, IMaintenancePaymentRepository paymentRepo)
    : IRequestHandler<CreatePaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        var ticket = await ticketRepo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");
        if (ticket.Status != TicketStatus.AwaitingPayment)
            throw new InvalidOperationException("Ticket is not awaiting payment.");
        var existing = await paymentRepo.GetByTicketIdAsync(cmd.TicketId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Payment already exists for this ticket.");
        var payment = MaintenancePayment.Create(cmd.TicketId, cmd.PayerId, cmd.VendorId,
            cmd.Amount, cmd.Currency, cmd.PaymentMethod);
        await paymentRepo.AddAsync(payment, ct);
        await paymentRepo.SaveChangesAsync(ct);
        return payment.Id;
    }
}