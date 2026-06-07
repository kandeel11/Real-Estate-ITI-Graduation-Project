using Domain.Entities;
using Maintenance.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class TicketVendorAssignment : BaseEntity
    {
        public Guid TicketId { get; private set; }
        public Guid VendorId { get; private set; }
        public Guid AssignedById { get; private set; }
        public AssignmentStatus Status { get; private set; } = AssignmentStatus.Pending;
        public string? RejectionReason { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public DateTime? RejectedAt { get; private set; }

        public MaintenanceTicket Ticket { get; private set; } = default!;

        protected TicketVendorAssignment() { }

        public static TicketVendorAssignment Create(Guid ticketId, Guid vendorId, Guid assignedById)
            => new() { TicketId = ticketId, VendorId = vendorId, AssignedById = assignedById };

        public void Accept()
        {
            if (Status != AssignmentStatus.Pending)
                throw new InvalidOperationException("Assignment is not pending.");
            Status = AssignmentStatus.Accepted;
            AcceptedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void Reject(string reason)
        {
            if (Status != AssignmentStatus.Pending)
                throw new InvalidOperationException("Assignment is not pending.");
            Status = AssignmentStatus.Rejected;
            RejectionReason = reason;
            RejectedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }
    }
}
