using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Commands.ApplyLateFee;

/// <summary>
/// Handler بتاع ApplyLateFeeCommand
/// 
/// بيطبق غرامة التأخير على الدفعة ويحول حالتها لـ Late
/// </summary>
public sealed class ApplyLateFeeHandler : IRequestHandler<ApplyLateFeeCommand, bool>
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<ApplyLateFeeHandler> _logger;

    public ApplyLateFeeHandler(
        IPaymentRepository repository,
        ILogger<ApplyLateFeeHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(ApplyLateFeeCommand request, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("ApplyLateFee: Payment {PaymentId} not found.", request.PaymentId);
            return false;
        }

        // الغرامة بتتطبق على الدفعات اللي لسه Pending أو Late
        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Late))
        {
            _logger.LogWarning(
                "ApplyLateFee: Payment {PaymentId} is in status {Status}. Late fee cannot be applied.",
                request.PaymentId, payment.Status);
            return false;
        }

        payment.ApplyLateFee(request.LateFeeAmount);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Late fee of {LateFee} applied to Payment {PaymentId}.",
            request.LateFeeAmount, request.PaymentId);

        return true;
    }
}
