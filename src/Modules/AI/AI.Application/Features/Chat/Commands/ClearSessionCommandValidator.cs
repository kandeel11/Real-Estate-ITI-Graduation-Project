using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Features.Chat.Commands
{
    public sealed class ClearSessionCommandValidator : AbstractValidator<ClearSessionCommand>
    {
        public ClearSessionCommandValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("SessionId is required.");
        }
    }
}
