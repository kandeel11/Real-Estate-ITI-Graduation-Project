using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Payment.Application.Features.Commands.ProcessPayment;

/// <summary>
/// Handler بتاع ProcessPaymentCommand
/// بعد الـ process بيبعت notification للتنانت
/// </summary>
public sealed class ProcessPaymentHandler
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ILeaseService _leaseService;
    private readonly INotificationService _notificationService;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(
        IPaymentRepository repository,
        IPaymentGatewayService paymentGateway,
        ILeaseService leaseService,
        INotificationService notificationService,
        IOutboxWriter outboxWriter,
        ILogger<ProcessPaymentHandler> logger)
    {
        _repository = repository;
        _paymentGateway = paymentGateway;
        _leaseService = leaseService;
        _notificationService = notificationService;
        _outboxWriter = outboxWriter;
        _logger = logger;
    }

    public async Task<ProcessPaymentResult> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
            return new ProcessPaymentResult(false, null, null,
                $"Payment with ID {request.PaymentId} not found.");

        if (payment.Status == PaymentStatus.Paid)
            return new ProcessPaymentResult(false, null, null,
                "Payment has already been processed.");

        _logger.LogInformation("Processing payment {Id} via {Method}", request.PaymentId, request.PaymentMethod);

        var gatewayRequest = new ProcessPaymentRequest(
            PaymentId: payment.Id,
            Amount: payment.Amount + payment.LateFee,
            PaymentMethod: request.PaymentMethod,
            CardToken: request.CardToken,
            Description: $"Rent payment for lease {payment.LeaseId}"
        );

        var gatewayResult = await _paymentGateway.ProcessPaymentAsync(gatewayRequest, cancellationToken);

        if (gatewayResult.IsSuccess)
        {
            payment.MarkAsPaid(gatewayResult.TransactionId!, request.PaymentMethod, gatewayResult.ReceiptUrl);

            // Record 1% platform commission on every rent payment
            if (payment.PaymentType == PaymentType.Rent)
                payment.RecordCommission();

            await _repository.SaveChangesAsync(cancellationToken);

            // Publish wallet-credit event via outbox (same as Stripe webhook path)
            if (payment.PaymentType == PaymentType.Rent && payment.RentDetails is not null)
            {
                var details = payment.RentDetails;
                var leaseDto = await _leaseService.GetLeaseByIdAsync(details.LeaseId, cancellationToken);
                await _outboxWriter.AddAsync(new RentPaymentSucceededEvent(
                    PaymentId: payment.Id,
                    OwnerId: details.LandlordId,
                    TenantId: details.TenantId,
                    LeaseId: details.LeaseId,
                    PropertyId: details.PropertyId,
                    Amount: payment.Amount,
                    CompanyId: leaseDto?.CompanyId
                ), cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
            }

            // إرسال إشعار للتنانت بعد نجاح الدفع
            await TrySendSuccessNotificationAsync(payment, gatewayResult, cancellationToken);
        }
        else
        {
            payment.MarkAsFailed();
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Payment {Id} failed. Reason={Reason}",
                request.PaymentId, gatewayResult.ErrorMessage);
        }

        return new ProcessPaymentResult(
            gatewayResult.IsSuccess, gatewayResult.TransactionId,
            gatewayResult.ReceiptUrl, gatewayResult.ErrorMessage);
    }

    private async Task TrySendSuccessNotificationAsync(
        Domain.Entities.Payment payment,
        PaymentGatewayResult gatewayResult,
        CancellationToken cancellationToken)
    {
        try
        {
            var lease = await _leaseService.GetLeaseByIdAsync(payment.LeaseId, cancellationToken);
            if (lease is null) return;

            var emailBody = $"""
                <h2>تم استلام دفعتك بنجاح</h2>
                <p>عزيزي {lease.TenantName}،</p>
                <p>تم استلام دفعة الإيجار بمبلغ <strong>{payment.Amount:C}</strong>.</p>
                <p>رقم المعاملة: <strong>{gatewayResult.TransactionId}</strong></p>
                {(gatewayResult.ReceiptUrl is not null ? $"<p><a href='{gatewayResult.ReceiptUrl}'>تحميل الإيصال</a></p>" : "")}
                """;

            await _notificationService.SendEmailAsync(new SendEmailRequest(
                ToEmail: lease.TenantEmail,
                ToName: lease.TenantName,
                Subject: "تم استلام دفعة الإيجار",
                Body: emailBody
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            // الـ notification failure لا توقف العملية الأساسية
            _logger.LogError(ex, "Failed to send payment success notification for PaymentId={Id}", payment.Id);
        }
    }
}
