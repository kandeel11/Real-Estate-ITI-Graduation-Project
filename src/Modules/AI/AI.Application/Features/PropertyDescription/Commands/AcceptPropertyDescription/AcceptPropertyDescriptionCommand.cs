using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription
{
    // FinalText* are optional: when null/blank the stored AI draft is accepted as-is.
    public record AcceptPropertyDescriptionCommand(Guid GenerationId,
        string? FinalTextEn = null,
        string? FinalTextAr = null) : IRequest<bool>;
}
