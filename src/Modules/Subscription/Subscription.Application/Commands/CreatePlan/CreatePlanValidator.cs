using FluentValidation;

namespace Subscription.Application.Commands.CreatePlan;

public class CreatePlanValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PriceMonthly).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PriceAnnual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxProperties).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxBrokers).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAiRequestsPerMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStorageGb).GreaterThanOrEqualTo(0);
    }
}
