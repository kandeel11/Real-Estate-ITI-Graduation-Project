using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Queries.GetPayment;

public record GetPaymentQuery(Guid TicketId) : IRequest<PaymentDto?>;

public class GetPaymentHandler(IMaintenancePaymentRepository repo)
    : IRequestHandler<GetPaymentQuery, PaymentDto?>
{
    public async Task<PaymentDto?> Handle(GetPaymentQuery q, CancellationToken ct)
    {
        var p = await repo.GetByTicketIdAsync(q.TicketId, ct);
        if (p is null) return null;
        return new PaymentDto(p.Id, p.Amount, p.Currency, p.Status.ToString(),
            p.PaymentMethod, p.TransactionId, p.PaidAt, p.CreatedAt,
            p.CheckoutUrl, p.CheckoutSessionId);
    }
}