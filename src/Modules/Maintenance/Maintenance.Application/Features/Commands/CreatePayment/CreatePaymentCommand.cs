using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Entities;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid TicketId,
    Guid VendorId,
    decimal Amount,
    string Currency = "EGP") : IRequest<Guid>;

public class CreatePaymentHandler(
    IMaintenanceTicketRepository ticketRepo,
    IMaintenancePaymentRepository paymentRepo)
    : IRequestHandler<CreatePaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentCommand cmd, CancellationToken ct)
    {
        var ticket = await ticketRepo.GetByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        // Vendor sets the price while work is in progress
        if (ticket.Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Ticket must be In Progress to set a price.");

        var existing = await paymentRepo.GetByTicketIdAsync(cmd.TicketId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Payment already exists for this ticket.");

        var payerId = ticket.ResponsibleParty == ResponsibleParty.Owner
            ? ticket.OwnerId
            : ticket.TenantId;

        var payment = MaintenancePayment.Create(
            cmd.TicketId, payerId, cmd.VendorId,
            cmd.Amount, cmd.Currency);

        await paymentRepo.AddAsync(payment, ct);

        // Setting the price marks the work as done and moves the ticket
        // to AwaitingPayment — the tenant/owner can now see the price and pay
        ticket.CompleteWork();

        await paymentRepo.SaveChangesAsync(ct);
        await ticketRepo.SaveChangesAsync(ct);

        return payment.Id;
    }
}