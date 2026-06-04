using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Features.Chat.Commands
{
    public sealed record SendMessageCommand(
     string SessionId,
     string Message,
     Guid? ContractId    // optional — scopes search to one contract
 ) : IRequest<SendMessageResult>;
}
