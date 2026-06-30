using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace Payment.Application.Features.Commands.MarkAsPaidExternally;

public sealed class MarkAsPaidExternallyHandler
    : IRequestHandler<MarkAsPaidExternallyCommand, bool>
{
    private readonly IPaymentRepository _repository;
    private readonly IPublisher _publisher;
    private readonly ILogger<MarkAsPaidExternallyHandler> _logger;

    public MarkAsPaidExternallyHandler(
        IPaymentRepository repository,
        IPublisher publisher,
        ILogger<MarkAsPaidExternallyHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<bool> Handle(
        MarkAsPaidExternallyCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            _logger.LogWarning("MarkAsPaidExternally: payment {Id} not found.", request.PaymentId);
            return false;
        }

        payment.MarkAsPaidExternally(request.Notes ?? "Marked as externally paid by admin.");

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payment {Id} (DueDate={DueDate}) marked as PaidExternally by admin.",
            payment.Id, payment.DueDate);

        // Auto-create the next installment (same chain as Stripe-paid payments)
        if (payment.PaymentType == PaymentType.Rent)
        {
            await _publisher.Publish(new RentPaymentPaidIntegrationEvent(
                PaymentId: payment.Id,
                LeaseId: payment.LeaseId,
                PaidDueDate: payment.DueDate
            ), cancellationToken);
        }

        return true;
    }
}
