using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

public record RentPaymentSucceededEvent(
    Guid PaymentId,
    Guid OwnerId,
    Guid TenantId,
    Guid LeaseId,
    Guid PropertyId,
    decimal Amount,
    Guid? CompanyId = null
) : INotification;
