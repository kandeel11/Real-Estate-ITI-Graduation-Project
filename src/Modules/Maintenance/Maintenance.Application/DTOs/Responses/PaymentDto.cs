using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.DTOs.Responses
{
    public record PaymentDto(Guid Id, decimal Amount, string Currency, string Status, string? PaymentMethod, string? TransactionId, DateTime? PaidAt, DateTime CreatedAt);
}
