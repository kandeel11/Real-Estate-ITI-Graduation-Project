using FluentValidation;

namespace Payment.Application.Features.Commands.RefundPayment;
public sealed class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID مطلوب.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("سبب الاسترداد مطلوب.")
            .MaximumLength(500)
            .WithMessage("سبب الاسترداد طويل جداً.");
    }
}
