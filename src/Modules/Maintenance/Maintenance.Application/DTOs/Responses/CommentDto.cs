using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record CommentDto(Guid Id, Guid UserId, string UserRole, string Content, bool IsInternal, DateTime CreatedAt, DateTime? UpdatedAt);
}
