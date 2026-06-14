using FluentValidation;

namespace Payment.Application.Features.Commands.UpdateAiPrediction;

public sealed class UpdateAiPredictionValidator : AbstractValidator<UpdateAiPredictionCommand>
{
    public UpdateAiPredictionValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID مطلوب.");
    }
}
