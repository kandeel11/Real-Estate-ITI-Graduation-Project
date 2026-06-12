using MediatR;

namespace Payment.Application.Features.Commands.UpdateAiPrediction;

public sealed record UpdateAiPredictionCommand(
    Guid PaymentId,
    bool PredictedLate
) : IRequest<bool>;
