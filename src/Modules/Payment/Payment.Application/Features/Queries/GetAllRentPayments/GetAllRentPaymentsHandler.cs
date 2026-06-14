using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetAllRentPayments;
public sealed class GetAllRentPaymentsHandler
    : IRequestHandler<GetAllRentPaymentsQuery, IReadOnlyList<RentPaymentDto>>
{
    private readonly IRentPaymentRepository _repository;

    public GetAllRentPaymentsHandler(IRentPaymentRepository repository)
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
                p.Id, p.LeaseId, p.Amount, p.DueDate, p.PaidAt,
                p.Status.ToString(), p.PaymentMethod, p.TransactionId,
                p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                p.CreatedAt, p.UpdatedAt))
            .ToList();
    }
}