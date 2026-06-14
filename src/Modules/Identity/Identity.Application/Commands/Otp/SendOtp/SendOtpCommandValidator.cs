// Identity.Application/Features/Auth/Otp/SendOtp/SendOtpCommandValidator.cs

using FluentValidation;

namespace Identity.Application.Features.Auth.Otp.SendOtp;

public sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
	public SendOtpCommandValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required.")
			.EmailAddress().WithMessage("Invalid email format.")
			.MaximumLength(256);
	}
}