using MediatR;

namespace Document.Application.Events;

public record LeaseExtractionData(
    decimal? InstallmentAmount,
    string? PaymentFrequency,
    decimal? TotalRentAmount,
    int? DurationInMonths,
    decimal? SecurityDeposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int? PaymentDayOfMonth,
    decimal? Confidence);

public record LeaseExtractionCompletedEvent(
    Guid LeaseId,
    LeaseExtractionData ExtractedData) : INotification;
