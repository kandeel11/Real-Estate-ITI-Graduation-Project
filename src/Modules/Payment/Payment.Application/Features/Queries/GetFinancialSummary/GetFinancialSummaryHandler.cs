using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Queries.GetFinancialSummary;
public sealed class GetFinancialSummaryHandler
    : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IRentPaymentRepository _repository;

    public GetFinancialSummaryHandler(IRentPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<FinancialSummaryDto> Handle(
        GetFinancialSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var allPayments = await _repository.GetAllAsync(cancellationToken);

        var totalCollected = allPayments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Sum(p => p.Amount);

        var totalPending = allPayments
            .Where(p => p.Status is PaymentStatus.Pending or PaymentStatus.Late)
            .Sum(p => p.Amount);

        var totalLateFees = allPayments
            .Sum(p => p.LateFee);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalOverdue = allPayments
            .Count(p => p.IsOverdue(today));

        return new FinancialSummaryDto(
            TotalCollected: totalCollected,
            TotalPending: totalPending,
            TotalLateFees: totalLateFees,
            TotalOverduePayments: totalOverdue
        );
    }
}
