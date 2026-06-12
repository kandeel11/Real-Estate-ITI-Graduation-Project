using FluentValidation;

namespace Payment.Application.Features.Commands.ApplyLateFee;

public sealed class ApplyLateFeeValidator : AbstractValidator<ApplyLateFeeCommand>
{
    public ApplyLateFeeValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID مطلوب.");

        RuleFor(x => x.LateFeeAmount)
            .GreaterThan(0)
            .WithMessage("غرامة التأخير لازم تكون أكبر من صفر.")
            .LessThanOrEqualTo(50_000)
            .WithMessage("غرامة التأخير كبيرة جداً.");
    }
}
