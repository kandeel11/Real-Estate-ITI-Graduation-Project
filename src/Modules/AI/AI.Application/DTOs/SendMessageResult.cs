using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public sealed record SendMessageResult(
     string SessionId,
     string Answer,
     int PromptTokens,
     int CompletionTokens
 );
}
