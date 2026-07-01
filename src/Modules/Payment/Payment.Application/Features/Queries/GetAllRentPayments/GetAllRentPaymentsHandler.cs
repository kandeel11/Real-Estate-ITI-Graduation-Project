using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetAllRentPayments;
public sealed class GetAllRentPaymentsHandler
    : IRequestHandler<GetAllRentPaymentsQuery, IReadOnlyList<RentPaymentDto>>
{
    private readonly IPaymentRepository _repository;

    public GetAllRentPaymentsHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RentPaymentDto>> Handle(
        GetAllRentPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var payments = request.LeaseId.HasValue
            ? await _repository.GetByLeaseIdAsync(request.LeaseId.Value, cancellationToken)
            : await _repository.GetAllAsync(cancellationToken);

        return payments
            .Select(p => new RentPaymentDto(
                p.Id, p.LeaseId, p.PropertyId, p.TenantId, p.LandlordId,
                p.Amount, p.DueDate, p.PaidAt,
                p.Status.ToString(), p.PaymentMethod.ToString(), p.TransactionId,
                p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                p.CreatedAt, p.UpdatedAt,
                p.CheckoutUrl, p.CheckoutSessionId))
            .ToList();
    }
}