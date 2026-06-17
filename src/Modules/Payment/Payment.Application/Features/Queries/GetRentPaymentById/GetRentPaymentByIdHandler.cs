using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Queries.GetRentPaymentById
{
    public sealed class GetRentPaymentByIdHandler
        : IRequestHandler<GetRentPaymentByIdQuery, RentPaymentDto?>
    {
        private readonly IPaymentRepository _repository;

        public GetRentPaymentByIdHandler(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<RentPaymentDto?> Handle(
            GetRentPaymentByIdQuery request,
            CancellationToken cancellationToken)
        {
            var payment = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (payment is null)
                return null;

            return new RentPaymentDto(
                payment.Id, payment.LeaseId, payment.Amount, payment.DueDate, payment.PaidAt,
                payment.Status.ToString(), payment.PaymentMethod.ToString(), payment.TransactionId,
                payment.LateFee, payment.AiPredictedLate, payment.ReceiptUrl, payment.Notes,
                payment.CreatedAt, payment.UpdatedAt);
        }
    }
}
