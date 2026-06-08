using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record VendorAssignmentDto(Guid Id, Guid VendorId, string Status, string? RejectionReason, DateTime? AcceptedAt, DateTime CreatedAt);
}
