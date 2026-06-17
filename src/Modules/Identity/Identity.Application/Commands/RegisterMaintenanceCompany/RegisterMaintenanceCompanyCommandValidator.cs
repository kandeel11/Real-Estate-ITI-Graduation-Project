using FluentValidation;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public sealed class RegisterMaintenanceCompanyCommandValidator
    : AbstractValidator<RegisterMaintenanceCompanyCommand>
{
    public RegisterMaintenanceCompanyCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");

        RuleFor(x => x.TempPassword)
            .NotEmpty().WithMessage("Temporary password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.");
    }
}
