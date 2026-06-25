namespace Maintenance.Application.Interfaces;

/// <summary>
/// Creates hosted payment links (Stripe Checkout Sessions) for maintenance fees.
/// Implemented in the Host layer using IStripeCheckoutService, keeping
/// Maintenance.Application free of Stripe SDK and Payment module references.
/// </summary>
public interface IMaintenancePaymentLinkService
{
    /// <summary>
    /// Returns a (CheckoutUrl, SessionId) tuple. The caller stores the SessionId
    /// on the MaintenancePayment record and redirects the payer to CheckoutUrl.
    /// </summary>
    Task<(string CheckoutUrl, string SessionId)> CreateAsync(
        Guid maintenancePaymentId,
        decimal amount,
        string currency,
        CancellationToken ct = default);
}
