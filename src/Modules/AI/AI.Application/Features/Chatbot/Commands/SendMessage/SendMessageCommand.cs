using AI.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.Features.Chatbot.Commands.SendMessage
{
    public record SendMessageCommand(Guid UserId,
    Guid? SessionId,
    string Message) : IRequest<SendMessageResponse>;
    
}
