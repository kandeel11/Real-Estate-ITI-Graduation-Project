using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Commands.UpdateAiPrediction;

public sealed class UpdateAiPredictionHandler : IRequestHandler<UpdateAiPredictionCommand, bool>
{
    private readonly IRentPaymentRepository _repository;
    private readonly ILogger<UpdateAiPredictionHandler> _logger;

    public UpdateAiPredictionHandler(
        IRentPaymentRepository repository,
        ILogger<UpdateAiPredictionHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateAiPredictionCommand request, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("UpdateAiPrediction: Payment {PaymentId} not found.", request.PaymentId);
            return false;
        }

        payment.UpdateAiPrediction(request.PredictedLate);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI prediction updated for Payment {PaymentId}. PredictedLate={PredictedLate}",
            request.PaymentId, request.PredictedLate);

        return true;
    }
}
