using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Application.DTOs
{
    public record SendMessageResponse(Guid SessionId, string ReplyText);
    
}
