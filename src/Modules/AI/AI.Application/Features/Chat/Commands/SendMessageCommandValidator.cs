using FluentValidation;

namespace AI.Application.Features.Chat.Commands
{
    public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("SessionId is required.");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(4000).WithMessage("Message must not exceed 4000 characters.");
        }
    }
}
