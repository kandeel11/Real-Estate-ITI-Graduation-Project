using FluentValidation;

namespace Payment.Application.Features.Commands.CreateSubscriptionPayment;

/// <summary>
/// Validator بتاع CreateSubscriptionPaymentCommand
/// </summary>
public sealed class CreateSubscriptionPaymentValidator
    : AbstractValidator<CreateSubscriptionPaymentCommand>
{
    public CreateSubscriptionPaymentValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId مطلوب.");

        RuleFor(x => x.SubscriptionId)
            .NotEmpty()
            .WithMessage("SubscriptionId مطلوب.");

        RuleFor(x => x.PlanId)
            .NotEmpty()
            .WithMessage("PlanId مطلوب.");

        RuleFor(x => x.BillingCycle)
            .IsInEnum()
            .WithMessage("BillingCycle لازم يكون Monthly, Quarterly أو Annual.");
    }
}
