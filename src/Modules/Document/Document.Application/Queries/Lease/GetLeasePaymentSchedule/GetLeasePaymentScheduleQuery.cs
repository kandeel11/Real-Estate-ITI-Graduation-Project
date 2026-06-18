using MediatR;

namespace Document.Application.Queries.Lease.GetLeasePaymentSchedule;

public record GetLeasePaymentScheduleQuery(Guid LeaseId) : IRequest<PaymentScheduleDto?>;

public record PaymentPeriodDto(
    int PeriodNumber,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    decimal Amount,
    bool IsCurrent,
    bool IsNext,
    bool IsPast
);

public record PaymentScheduleSummaryDto(
    int TotalInstallments,
    int PastInstallments,
    int CurrentInstallments,
    int FutureInstallments,
    decimal TotalAmount,
    decimal AmountDueSoFar,
    decimal AmountRemaining
);

public record PaymentScheduleDto(
    Guid LeaseId,
    decimal InstallmentAmount,
    string PaymentFrequency,
    decimal TotalRentAmount,
    int DurationInMonths,
    DateOnly StartDate,
    DateOnly EndDate,
    int PaymentDayOfMonth,
    PaymentPeriodDto? CurrentPeriod,
    PaymentPeriodDto? NextPeriod,
    IReadOnlyList<PaymentPeriodDto> AllPeriods,
    PaymentScheduleSummaryDto Summary
);
