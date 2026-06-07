using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription
{
    public record AcceptPropertyDescriptionCommand(Guid GenerationId,
        string FinalTextEn, 
        string FinalTextAr) : IRequest<bool>;
}
