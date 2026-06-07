using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Features.Chat.Commands
{
    public sealed record SendMessageCommand(
     Guid? SessionId,    // ← Guid? not string?
     Guid UserId,
     string Message,
     Guid? ContractId
 ) : IRequest<SendMessageResult>;
}
