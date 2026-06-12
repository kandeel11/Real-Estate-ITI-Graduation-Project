using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Application.Features.Queries.GetOverduePayments
{
    public sealed class GetOverduePaymentsHandler
        : IRequestHandler<GetOverduePaymentsQuery, IReadOnlyList<RentPaymentDto>>
    {
        private readonly IRentPaymentRepository _repository;

        public GetOverduePaymentsHandler(IRentPaymentRepository repository)
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
                    p.Id, p.LeaseId, p.Amount, p.DueDate, p.PaidAt,
                    p.Status.ToString(), p.PaymentMethod, p.TransactionId,
                    p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                    p.CreatedAt, p.UpdatedAt))
                .ToList();
        }
    }
}
