using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.WaiveLateFee;

public sealed class WaiveLateFeeHandler : IRequestHandler<WaiveLateFeeCommand, bool>
{
    private readonly IPaymentRepository     _repo;
    private readonly IStripeCheckoutService _stripe;
    private readonly ILeaseService          _leaseService;
    private readonly ILogger<WaiveLateFeeHandler> _logger;

    public WaiveLateFeeHandler(
        IPaymentRepository repo,
        IStripeCheckoutService stripe,
        ILeaseService leaseService,
        ILogger<WaiveLateFeeHandler> logger)
    {
        _repo         = repo;
        _stripe       = stripe;
        _leaseService = leaseService;
        _logger       = logger;
    }

    public async Task<bool> Handle(WaiveLateFeeCommand request, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, ct);

        if (payment is null || payment.PaymentType != PaymentType.Rent)
            return false;

        if (payment.Status == PaymentStatus.Paid)
            return false;

        // Landlords may only waive fees on their own properties
        if (request.RequesterRole == "owner" && payment.LandlordId != request.RequesterId)
            return false;

        if (payment.LateFee <= 0)
            return false;

        payment.WaiveLateFee();

        // Regenerate Stripe checkout link at the base amount (no late fee), suffix _wlf
        // so it gets a fresh idempotency key and Stripe creates a new session.
        try
        {
            var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
            var total = Math.Round(payment.Amount * 1.01m, 2);

            var result = await _stripe.CreateCheckoutSessionAsync(new(
                PaymentId:   payment.Id,
                Amount:      total,
                ProductName: $"Rent Payment — {payment.DueDate:MMMM yyyy}",
                SuccessUrl:  request.SuccessUrl,
                CancelUrl:   request.CancelUrl,
                Metadata: new Dictionary<string, string>
                {
                    ["payment_id"]  = payment.Id.ToString(),
                    ["lease_id"]    = payment.LeaseId.ToString(),
                    ["due_date"]    = payment.DueDate.ToString("yyyy-MM-dd"),
                    ["type"]        = "rent",
                    ["tenant_id"]   = lease?.TenantId.ToString() ?? string.Empty,
                    ["property_id"] = lease?.PropertyId.ToString() ?? string.Empty,
                    ["company_id"]  = lease?.CompanyId?.ToString() ?? string.Empty
                },
                IdempotencySuffix: "_wlf"
            ), ct);

            payment.SetCheckoutSession(result.SessionId, result.Url);

            _logger.LogInformation(
                "WaiveLateFeeHandler: Stripe link regenerated for Payment {Id}, Amount={Amount}",
                payment.Id, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "WaiveLateFeeHandler: Failed to regenerate Stripe link for Payment {Id}. Fee still waived.",
                payment.Id);
        }

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Late fee waived for Payment {PaymentId} by {Role} {RequesterId}",
            payment.Id, request.RequesterRole, request.RequesterId);

        return true;
    }
}
