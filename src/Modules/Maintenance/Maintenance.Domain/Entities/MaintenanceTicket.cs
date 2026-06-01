using Maintenance.Domain.Enums;
using Maintenance.Domain.Events;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maintenance.Domain.Entities
{
    public class MaintenanceTicket : BaseEntity   
    {
        public Guid PropertyId { get; private set; }
        public Guid? LeaseId { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid? MaintenanceCompanyId { get; private set; }
        public string Title { get; private set; } = default!;
        public string Description { get; private set; } = default!;
        public TicketPriority Priority { get; private set; }
        public TicketStatus Status { get; private set; } = TicketStatus.Open;
        public string? AiPredictedSla { get; private set; }
        public DateTime? ResolvedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }

        // ── Navigation properties ────────────────────────────────────────────────
        public ICollection<MaintenanceTicketAttachment> Attachments { get; private set; } = [];
        public ICollection<MaintenanceTicketComment> Comments { get; private set; } = [];
        protected MaintenanceTicket() { }

        public static MaintenanceTicket Create(
            Guid propertyId,
            Guid tenantId,
            string title,
            string description,
            TicketPriority priority,
            Guid? leaseId = null)
        {
            var ticket = new MaintenanceTicket
            {
                PropertyId = propertyId,
                TenantId = tenantId,
                LeaseId = leaseId,
                Title = title,
                Description = description,
                Priority = priority
            };
            ticket.AddDomainEvent(new TicketCreatedDomainEvent(ticket.Id, tenantId, propertyId));

            return ticket;
        }

        public void TransitionTo(TicketStatus newStatus)
        {
            var valid = (Status, newStatus) switch
            {
                (TicketStatus.Open, TicketStatus.Assigned) => true,
                (TicketStatus.Open, TicketStatus.Cancelled) => true,
                (TicketStatus.Assigned, TicketStatus.InProgress) => true,
                (TicketStatus.Assigned, TicketStatus.Cancelled) => true,
                (TicketStatus.InProgress, TicketStatus.Resolved) => true,
                (TicketStatus.InProgress, TicketStatus.Cancelled) => true,
                (TicketStatus.Resolved, TicketStatus.Closed) => true,
                (TicketStatus.Resolved, TicketStatus.InProgress) => true, 
                _ => false
            };

            if (!valid)
                throw new InvalidOperationException(
                    $"Cannot transition from {Status} to {newStatus}.");

            var previousStatus = Status;

            Status = newStatus;
            MarkAsUpdated();  

            if (newStatus == TicketStatus.Resolved) ResolvedAt = DateTime.UtcNow;
            if (newStatus == TicketStatus.Closed) ClosedAt = DateTime.UtcNow;

            AddDomainEvent(new TicketStatusChangedDomainEvent(Id, previousStatus, newStatus));
        }

        public void AssignVendor(Guid companyId)
        {
            MaintenanceCompanyId = companyId;
            TransitionTo(TicketStatus.Assigned);
            AddDomainEvent(new VendorAssignedDomainEvent(Id, companyId));
        }

        public void SetAiPrediction(TicketPriority priority, string sla)
        {
            Priority = priority;
            AiPredictedSla = sla;
            MarkAsUpdated();  // BaseEntity
        }

        public void SoftDelete() => MarkAsDeleted();
    }
}