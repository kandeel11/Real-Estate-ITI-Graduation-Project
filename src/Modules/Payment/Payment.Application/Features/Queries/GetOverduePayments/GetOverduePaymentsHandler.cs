using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetOverduePayments
{
    public sealed class GetOverduePaymentsHandler
        : IRequestHandler<GetOverduePaymentsQuery, IReadOnlyList<RentPaymentDto>>
    {
        private readonly IPaymentRepository _repository;

        public GetOverduePaymentsHandler(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<RentPaymentDto>> Handle(
            GetOverduePaymentsQuery request,
            CancellationToken cancellationToken)
        {
            var overduePayments = await _repository.GetOverdueAsync(cancellationToken);

            return overduePayments
                .Select(p => new RentPaymentDto(
                    p.Id, p.LeaseId, p.PropertyId, p.TenantId, p.LandlordId,
                    p.Amount, p.DueDate, p.PaidAt,
                    p.Status.ToString(), p.PaymentMethod.ToString(), p.TransactionId,
                    p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                    p.CreatedAt, p.UpdatedAt))
                .ToList();
        }
    }
}
