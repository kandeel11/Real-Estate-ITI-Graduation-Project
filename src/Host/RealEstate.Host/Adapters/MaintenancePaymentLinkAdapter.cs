using Maintenance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Payment.Application.Interfaces;

namespace RealEstate.Host.Adapters;

/// <summary>
/// Bridges Payment.Application's IStripeCheckoutService into
/// Maintenance.Application's IMaintenancePaymentLinkService.
/// Uses "maintenance_payment_id" metadata so the webhook can distinguish
/// maintenance payments from rent/subscription ones.
/// </summary>
public sealed class MaintenancePaymentLinkAdapter(
    IStripeCheckoutService stripeCheckout,
    IConfiguration configuration)
    : IMaintenancePaymentLinkService
{
    public async Task<(string CheckoutUrl, string SessionId)> CreateAsync(
        Guid maintenancePaymentId,
        decimal amount,
        string currency,
        CancellationToken ct = default)
    {
        var baseUrl = configuration["EmailSettings:AppBaseUrl"]?.TrimEnd('/')
                      ?? "https://localhost:7240";

        var request = new CreateCheckoutSessionRequest(
            PaymentId: maintenancePaymentId,
            Amount: amount,
            ProductName: "Maintenance Fee",
            SuccessUrl: $"{baseUrl}/maintenance/payment-success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl: $"{baseUrl}/maintenance/payment-cancelled",
            Metadata: new Dictionary<string, string>
            {
                ["maintenance_payment_id"] = maintenancePaymentId.ToString()
            });

        var result = await stripeCheckout.CreateCheckoutSessionAsync(request, ct);
        return (result.Url, result.SessionId);
    }
}
