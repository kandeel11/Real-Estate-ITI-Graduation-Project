using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record LeaseActivatedIntegrationEvent(
    Guid LeaseId,
    Guid PropertyId,
    Guid OwnerId,
    Guid TenantId,
    decimal InstallmentAmount,
    string PaymentFrequency,
    int DurationInMonths,
    int PaymentDayOfMonth,
    DateOnly StartDate,
    DateOnly EndDate
) : INotification;
