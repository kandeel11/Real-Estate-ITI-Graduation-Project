using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record StatusHistoryItemDto(string FromStatus, string ToStatus, string ChangedBy, string? Notes, DateTime ChangedAt);
}
