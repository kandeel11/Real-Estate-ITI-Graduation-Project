using FluentValidation;

namespace Payment.Application.Features.Commands.ProcessPayment;

public sealed class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    private static readonly string[] AllowedMethods =
        ["CreditCard", "DebitCard", "BankTransfer", "Cash", "Fawry", "PayMob", "Vodafone"];

    public ProcessPaymentValidator()
    {
        RuleFor(x => x.PaymentId)
            .NotEmpty()
            .WithMessage("Payment ID مطلوب.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("طريقة الدفع مطلوبة.")
            .Must(m => AllowedMethods.Contains(m))
            .WithMessage($"طريقة الدفع لازم تكون إحدى: {string.Join(", ", AllowedMethods)}.");
    }
}
