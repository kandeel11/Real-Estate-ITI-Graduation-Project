using Maintenance.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Application.Interfaces
{
    public interface IMaintenancePaymentRepository
    {
        Task<MaintenancePayment?> GetByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
        Task<MaintenancePayment?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<List<MaintenancePayment>> GetAllPaidAsync(CancellationToken ct = default);
        Task AddAsync(MaintenancePayment payment, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
