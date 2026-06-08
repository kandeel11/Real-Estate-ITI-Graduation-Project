using Maintenance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.Interfaces
{
    public interface IVendorAssignmentRepository
    {
        Task<TicketVendorAssignment?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<TicketVendorAssignment?> GetCurrentByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
        Task<List<TicketVendorAssignment>> GetAllByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
        Task AddAsync(TicketVendorAssignment assignment, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
