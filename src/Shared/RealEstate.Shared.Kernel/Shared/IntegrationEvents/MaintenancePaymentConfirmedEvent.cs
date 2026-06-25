using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published by StripeWebhookService when a maintenance Checkout Session completes.
/// Handled by Maintenance.Application to confirm the payment and close the ticket.
/// </summary>
public sealed record MaintenancePaymentConfirmedEvent(
    Guid MaintenancePaymentId,
    string StripeTransactionId) : INotification;
