using Subscription.Domain.Enums;

namespace Subscription.Application.DTOs;

public record SubscriptionPaymentDto
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public SubscriptionPaymentStatus Status { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime DueDate { get; init; }
    public string? InvoiceUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}
