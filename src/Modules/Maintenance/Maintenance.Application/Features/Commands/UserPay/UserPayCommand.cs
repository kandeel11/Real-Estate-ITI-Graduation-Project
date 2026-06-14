using MediatR;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Commands.UserPay;

public record UserPayCommand(Guid PaymentId, string TransactionId) : IRequest;

public class UserPayHandler(IMaintenancePaymentRepository paymentRepo)
    : IRequestHandler<UserPayCommand>
{
    public async Task Handle(UserPayCommand cmd, CancellationToken ct)
    {
        var payment = await paymentRepo.GetByIdAsync(cmd.PaymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {cmd.PaymentId} not found.");

        payment.MarkUserPaid(cmd.TransactionId);
        await paymentRepo.SaveChangesAsync(ct);
    }
}
