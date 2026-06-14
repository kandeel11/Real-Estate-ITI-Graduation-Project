using MediatR;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Queries.GetMyPayments;

public class GetMyPaymentsHandler
    : IRequestHandler<GetMyPaymentsQuery, List<SubscriptionPaymentItemDto>?>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ISubscriptionPaymentRepository _payments;

    public GetMyPaymentsHandler(
        ISubscriptionRepository subscriptions,
        ISubscriptionPaymentRepository payments)
    {
        _subscriptions = subscriptions;
        _payments = payments;
    }

    public async Task<List<SubscriptionPaymentItemDto>?> Handle(
        GetMyPaymentsQuery request, CancellationToken cancellationToken)
    {
        var subscription = request.CompanyId is not null
            ? await _subscriptions.GetActiveByCompanyAsync(request.CompanyId.Value, cancellationToken)
            : await _subscriptions.GetActiveByUserAsync(request.UserId, cancellationToken);

        // null = no subscription (controller maps to 404); empty list = no payments yet
        if (subscription is null) return null;

        var payments = await _payments.GetBySubscriptionAsync(subscription.Id, cancellationToken);

        return payments
            .OrderByDescending(p => p.DueDate)
            .Select(p => new SubscriptionPaymentItemDto(
                p.Id,
                p.Amount,
                p.Currency,
                p.PaymentMethod,
                p.Status.ToString(),
                p.PaidAt,
                p.DueDate,
                p.InvoiceUrl,
                p.CreatedAt))
            .ToList();
    }
}
