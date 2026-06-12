using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.RefundPayment;
public sealed class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, bool>
{
    private readonly IRentPaymentRepository _repository;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(IRentPaymentRepository repository, ILogger<RefundPaymentHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("Refund: Payment {Id} not found.", request.PaymentId);
            return false;
        }

        if (payment.Status != PaymentStatus.Paid)
        {
            _logger.LogWarning(
                "Refund: Payment {Id} is {Status}, only Paid payments can be refunded.",
                request.PaymentId, payment.Status);
            return false;
        }

        payment.MarkAsRefunded(request.Reason);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment {Id} refunded. Reason: {Reason}", request.PaymentId, request.Reason);
        return true;
    }
}
