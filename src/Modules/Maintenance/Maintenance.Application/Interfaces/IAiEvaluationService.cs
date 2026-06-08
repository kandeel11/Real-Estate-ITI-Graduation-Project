using Maintenance.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.Interfaces
{
    public interface IAiEvaluationService
    {
        Task<AiEvaluationResultDto> EvaluateAsync(
        string ticketNumber, string leaseId,
        string description, string imageUrl,
        CancellationToken ct = default);
    }
}
