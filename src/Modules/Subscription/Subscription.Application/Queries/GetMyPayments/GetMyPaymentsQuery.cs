using MediatR;

namespace Subscription.Application.Queries.GetMyPayments;

/// <summary>
/// Payment history (invoices) for the caller's active subscription.
/// Status serialized as string, matching MySubscriptionDto.
/// </summary>
public record SubscriptionPaymentItemDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    DateTime? PaidAt,
    DateTime DueDate,
    string? InvoiceUrl,
    DateTime CreatedAt);

public record GetMyPaymentsQuery(Guid UserId, Guid? CompanyId)
    : IRequest<List<SubscriptionPaymentItemDto>?>;
