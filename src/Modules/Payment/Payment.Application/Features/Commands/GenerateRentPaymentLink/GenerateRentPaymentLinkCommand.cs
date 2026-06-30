using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.GenerateRentPaymentLink;

public record GenerateRentPaymentLinkCommand(
    Guid PaymentId,
    string SuccessUrl,
    string CancelUrl) : IRequest<GenerateRentPaymentLinkResult>;

public record GenerateRentPaymentLinkResult(bool Success, string? CheckoutUrl, string? Error);

public sealed class GenerateRentPaymentLinkHandler
    : IRequestHandler<GenerateRentPaymentLinkCommand, GenerateRentPaymentLinkResult>
{
    private readonly IPaymentRepository     _repo;
    private readonly IStripeCheckoutService _stripe;
    private readonly ILeaseService          _leaseService;
    private readonly ILogger<GenerateRentPaymentLinkHandler> _logger;

    public GenerateRentPaymentLinkHandler(
        IPaymentRepository repo,
        IStripeCheckoutService stripe,
        ILeaseService leaseService,
        ILogger<GenerateRentPaymentLinkHandler> logger)
    {
        _repo         = repo;
        _stripe       = stripe;
        _leaseService = leaseService;
        _logger       = logger;
    }

    public async Task<GenerateRentPaymentLinkResult> Handle(
        GenerateRentPaymentLinkCommand request, CancellationToken ct)
    {
        var payment = await _repo.GetByIdAsync(request.PaymentId, ct);

        if (payment is null)
            return new(false, null, "Payment not found.");

        if (payment.PaymentType != PaymentType.Rent)
            return new(false, null, "Only rent payments can have a Stripe link generated.");

        if (payment.Status is PaymentStatus.Paid or PaymentStatus.PaidExternally or PaymentStatus.Refunded)
            return new(false, null, $"Payment is already {payment.Status}.");

        try
        {
            var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, ct);
            var total = Math.Round((payment.Amount + payment.LateFee) * 1.01m, 2);

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
                IdempotencySuffix: $"_adm_{DateTime.UtcNow:yyyyMMddHH}"
            ), ct);

            payment.SetCheckoutSession(result.SessionId, result.Url);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Admin generated Stripe link for RentPayment {Id}, Amount={Amount}, Url={Url}",
                payment.Id, total, result.Url);

            return new(true, result.Url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Stripe link for RentPayment {Id}", payment.Id);
            return new(false, null, "Failed to generate payment link. Check Stripe configuration.");
        }
    }
}
