using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Features.Chat.Commands
{
    public sealed record ClearSessionCommand(
    Guid SessionId
) : IRequest;
}
