using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published after any rent payment transitions to Paid or PaidExternally.
/// Triggers creation of the next installment inside the Payment module.
/// </summary>
public record RentPaymentPaidIntegrationEvent(
    Guid PaymentId,
    Guid LeaseId,
    DateOnly PaidDueDate
) : INotification;
