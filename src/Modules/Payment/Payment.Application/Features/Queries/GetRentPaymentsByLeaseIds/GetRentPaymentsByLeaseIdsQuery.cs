using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetRentPaymentsByLeaseIds;

public sealed record GetRentPaymentsByLeaseIdsQuery(List<Guid> LeaseIds)
    : IRequest<IReadOnlyList<RentPaymentDto>>;

public sealed class GetRentPaymentsByLeaseIdsHandler
    : IRequestHandler<GetRentPaymentsByLeaseIdsQuery, IReadOnlyList<RentPaymentDto>>
{
    private readonly IPaymentRepository _repository;

    public GetRentPaymentsByLeaseIdsHandler(IPaymentRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<RentPaymentDto>> Handle(
        GetRentPaymentsByLeaseIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.LeaseIds.Count == 0) return [];

        var payments = await _repository.GetByLeaseIdsAsync(request.LeaseIds, cancellationToken);

        return payments
            .Select(p => new RentPaymentDto(
                p.Id, p.LeaseId, p.PropertyId, p.TenantId, p.LandlordId,
                p.Amount, p.DueDate, p.PaidAt,
                p.Status.ToString(), p.PaymentMethod.ToString(), p.TransactionId,
                p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                p.CreatedAt, p.UpdatedAt))
            .ToList();
    }
}
