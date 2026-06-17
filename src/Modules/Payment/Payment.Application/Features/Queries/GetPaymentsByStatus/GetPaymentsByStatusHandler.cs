using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetPaymentsByStatus
{
    public sealed class GetPaymentsByStatusHandler
    : IRequestHandler<GetPaymentsByStatusQuery, IReadOnlyList<RentPaymentDto>>
    {
        private readonly IPaymentDbContext _context;

        public GetPaymentsByStatusHandler(IPaymentDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RentPaymentDto>> Handle(
            GetPaymentsByStatusQuery request,
            CancellationToken cancellationToken)
        {
            var status = Enum.Parse<PaymentStatus>(request.Status, ignoreCase: true);

            var payments = await _context.Payments
                .Where(p => p.Status == status && p.PaymentType == PaymentType.Rent)
                .Include(p => p.RentDetails)
                .OrderByDescending(p => p.DueDate)
                .Select(p => new RentPaymentDto(
                    p.Id, p.RentDetails!.LeaseId, p.Amount, p.DueDate, p.PaidAt,
                    p.Status.ToString(), p.PaymentMethod.ToString(), p.TransactionId,
                    p.LateFee, p.AiPredictedLate, p.ReceiptUrl, p.Notes,
                    p.CreatedAt, p.UpdatedAt))
                .ToListAsync(cancellationToken);

            return payments;
        }
    }
}
