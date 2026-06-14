using FluentValidation;

namespace Payment.Application.Features.Commands.GenerateMonthlyPayments;

public sealed class GenerateMonthlyPaymentsValidator
    : AbstractValidator<GenerateMonthlyPaymentsCommand>
{
    public GenerateMonthlyPaymentsValidator()
    {
        When(x => x.TargetMonth.HasValue, () =>
        {
            RuleFor(x => x.TargetMonth!.Value.Year)
                .GreaterThanOrEqualTo(2000)
                .WithMessage("السنة لازم تكون 2000 أو أكتر.");

            RuleFor(x => x.TargetMonth!.Value.Month)
                .InclusiveBetween(1, 12)
                .WithMessage("الشهر لازم يكون بين 1 و 12.");
        });
    }
}
