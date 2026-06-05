using Domain.Entities;
using Maintenance.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class TicketStatusHistory : BaseEntity
    {
        public Guid TicketId { get; private set; }
        public TicketStatus FromStatus { get; private set; }
        public TicketStatus ToStatus { get; private set; }
        public string ChangedBy { get; private set; } = default!;
        public string? Notes { get; private set; }

        public MaintenanceTicket Ticket { get; private set; } = default!;

        protected TicketStatusHistory() { }

        public static TicketStatusHistory Create(
            Guid ticketId, TicketStatus from, TicketStatus to,
            string changedBy, string? notes = null)
            => new()
            {
                TicketId = ticketId,
                FromStatus = from,
                ToStatus = to,
                ChangedBy = changedBy,
                Notes = notes
            };
    }
}
