using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetMyRentPayments;

public sealed class GetMyRentPaymentsHandler
    : IRequestHandler<GetMyRentPaymentsQuery, IReadOnlyList<RentPaymentDto>>
{
    private readonly IPaymentRepository _repository;

    public GetMyRentPaymentsHandler(IPaymentRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<RentPaymentDto>> Handle(
        GetMyRentPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var payments = request.Role.Equals("owner", StringComparison.OrdinalIgnoreCase)
            ? await _repository.GetByLandlordIdAsync(request.UserId, cancellationToken)
            : await _repository.GetByTenantIdAsync(request.UserId, cancellationToken);

        return payments
            .Select(p => new RentPaymentDto(
                p.Id, p.LeaseId, p.PropertyId, p.TenantId, p.LandlordId,
                p.Amount, p.DueDate, p.PaidAt,
                p.Status.ToString(), p.PaymentMethod.ToString(), p.TransactionId,
                p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                p.CreatedAt, p.UpdatedAt,
                CheckoutUrl: p.CheckoutUrl,
                CheckoutSessionId: p.CheckoutSessionId))
            .ToList();
    }
}
