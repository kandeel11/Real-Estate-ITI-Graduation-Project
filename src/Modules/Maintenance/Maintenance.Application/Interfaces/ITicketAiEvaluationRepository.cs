using Maintenance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.Interfaces
{
    public interface ITicketAiEvaluationRepository
    {
        Task<TicketAiEvaluation?> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
        Task AddAsync(TicketAiEvaluation evaluation, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
