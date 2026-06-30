using MediatR;
using Maintenance.Application.Interfaces;
using Maintenance.Domain.Enums;

namespace Maintenance.Application.Features.Commands.PayFromWallet;

public record PayFromWalletCommand(
    Guid PaymentId,
    Guid PayerId,
    bool IsCompany,
    Guid? CompanyId = null) : IRequest;

public class PayFromWalletHandler(
    IMaintenancePaymentRepository paymentRepo,
    IMaintenanceTicketRepository ticketRepo,
    IWalletDeductionService walletService)
    : IRequestHandler<PayFromWalletCommand>
{
    public async Task Handle(PayFromWalletCommand cmd, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {cmd.PaymentId} not found.");

        if (payment.Status != MaintenancePaymentStatus.Pending)
            throw new InvalidOperationException("This payment has already been settled.");

        // Resolve wallet balance
        decimal balance = cmd.IsCompany && cmd.CompanyId.HasValue
            ? await walletService.GetCompanyBalanceAsync(cmd.CompanyId.Value, ct)
            : await walletService.GetOwnerBalanceAsync(cmd.PayerId, ct);

        if (balance < payment.Amount)
            throw new InvalidOperationException(
                $"Insufficient wallet balance. Available: {balance:0.##} {payment.Currency}, " +
                $"Required: {payment.Amount:0.##} {payment.Currency}.");

        var ticket = await ticketRepo.GetByIdAsync(payment.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {payment.TicketId} not found.");

        // Commission: 2% to platform, 98% recorded as net
        var commission = Math.Round(payment.Amount * 0.02m, 2);
        var description =
            $"Maintenance payment — Ticket #{ticket.TicketNumber} · " +
            $"{payment.Amount:0.##} {payment.Currency} " +
            $"(2% platform commission: {commission:0.##} {payment.Currency})";

        // Deduct from wallet
        if (cmd.IsCompany && cmd.CompanyId.HasValue)
            await walletService.DebitCompanyAsync(cmd.CompanyId.Value, cmd.PaymentId, payment.Amount, description, ct);
        else
            await walletService.DebitOwnerAsync(cmd.PayerId, cmd.PaymentId, payment.Amount, description, ct);

        // Confirm payment and complete the ticket in one step (no vendor confirmation needed)
        payment.ConfirmFromWallet();
        ticket.ConfirmPayment();

        await paymentRepo.SaveChangesAsync(ct);
        await ticketRepo.SaveChangesAsync(ct);
    }
}
