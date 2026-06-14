// Identity.Application/Features/Auth/Otp/VerifyOtp/VerifyOtpCommandValidator.cs

using FluentValidation;

namespace Identity.Application.Features.Auth.Otp.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6).WithMessage("OTP must be exactly 6 digits.")
            .Matches("^[0-9]{6}$").WithMessage("OTP must contain digits only.");
    }
}