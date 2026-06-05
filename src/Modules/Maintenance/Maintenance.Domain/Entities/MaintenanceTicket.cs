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
        public Guid OwnerId { get; private set; }
        public Guid? MaintenanceCompanyId { get; private set; }

        public string TicketNumber { get; private set; } = default!;
        public string Title { get; private set; } = default!;
        public string Description { get; private set; } = default!;
        public TicketPriority Priority { get; private set; }
        public TicketStatus Status { get; private set; } = TicketStatus.Draft;

        public string? PropertyAddress { get; private set; }
        public string? TenantEmail { get; private set; }
        public string? TenantPhone { get; private set; }

        public ResponsibleParty? ResponsibleParty { get; private set; }
        public ResolutionMethod? ResolutionMethod { get; private set; }

        public DateTime? ResolvedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }

        public ICollection<MaintenanceTicketAttachment> Attachments { get; private set; } = [];
        public ICollection<MaintenanceTicketComment> Comments { get; private set; } = [];
        public ICollection<TicketStatusHistory> StatusHistory { get; private set; } = [];
        public TicketAiEvaluation? AiEvaluation { get; private set; }
        public TicketVendorAssignment? CurrentVendorAssignment { get; private set; }
        public MaintenancePayment? Payment { get; private set; }

        protected MaintenanceTicket() { }

        public static MaintenanceTicket Create(
            Guid propertyId, Guid tenantId, Guid ownerId,
            string title, string description, TicketPriority priority,
            Guid? leaseId = null)
        {
            var ticket = new MaintenanceTicket
            {
                PropertyId = propertyId,
                TenantId = tenantId,
                OwnerId = ownerId,
                LeaseId = leaseId,
                Title = title,
                Description = description,
                Priority = priority,
                Status = TicketStatus.Draft,
                TicketNumber = string.Empty
            };
            ticket.AddDomainEvent(new TicketCreatedDomainEvent(ticket.Id, tenantId, propertyId));
            return ticket;
        }

        public void Submit(string ticketNumber, string propertyAddress,
                           string tenantEmail, string tenantPhone)
        {
            if (Status != TicketStatus.Draft)
                throw new InvalidOperationException("Only Draft tickets can be submitted.");
            if (!Attachments.Any(a => a.FileType.StartsWith("image/") && a.DeletedAt == null))
                throw new InvalidOperationException("At least one image attachment is required.");

            TicketNumber = ticketNumber;
            PropertyAddress = propertyAddress;
            TenantEmail = tenantEmail;
            TenantPhone = tenantPhone;

            TransitionTo(TicketStatus.Submitted, "System");
            AddDomainEvent(new TicketSubmittedDomainEvent(Id, TenantId, OwnerId, TicketNumber));
        }

        public void Update(string? title, string? description, TicketPriority? priority)
        {
            if (Status != TicketStatus.Draft && Status != TicketStatus.Submitted)
                throw new InvalidOperationException("Ticket can only be edited in Draft or Submitted status.");

            if (title is not null) Title = title;
            if (description is not null) Description = description;
            if (priority is not null) Priority = priority.Value;
            MarkAsUpdated();
        }

        public void ApplyAiEvaluation(ResponsibleParty responsible)
        {
            ResponsibleParty = responsible;
            TransitionTo(TicketStatus.AiEvaluated, "System");
            var next = responsible == Enums.ResponsibleParty.Owner
                ? TicketStatus.WaitingForOwnerDecision
                : TicketStatus.WaitingForTenantDecision;
            TransitionTo(next, "System");
        }

        public void MakeDecision(ResolutionMethod method)
        {
            if (Status != TicketStatus.WaitingForOwnerDecision
                && Status != TicketStatus.WaitingForTenantDecision)
                throw new InvalidOperationException("Decision can only be made while waiting for decision.");

            ResolutionMethod = method;
            if (method == Enums.ResolutionMethod.ExternalTechnician)
                TransitionTo(TicketStatus.InProgress, "Owner/Tenant");
            MarkAsUpdated();
        }

        public TicketVendorAssignment AssignVendor(Guid vendorId, Guid assignedById)
        {
            if (ResolutionMethod != Enums.ResolutionMethod.PlatformVendor)
                throw new InvalidOperationException("Resolution method must be PlatformVendor.");
            if (Status != TicketStatus.WaitingForOwnerDecision
                && Status != TicketStatus.WaitingForTenantDecision)
                throw new InvalidOperationException("Cannot assign vendor in current status.");

            var assignment = TicketVendorAssignment.Create(Id, vendorId, assignedById);
            TransitionTo(TicketStatus.AssignedToVendor, assignedById.ToString());
            AddDomainEvent(new VendorAssignedDomainEvent(Id, vendorId, assignment.Id));
            return assignment;
        }

        public void VendorAccepted()
        {
            TransitionTo(TicketStatus.VendorAccepted, "Vendor");
            AddDomainEvent(new VendorAssignmentAcceptedDomainEvent(Id, CurrentVendorAssignment!.VendorId));
        }

        public void VendorRejected()
        {
            var next = ResponsibleParty == Enums.ResponsibleParty.Owner
                ? TicketStatus.WaitingForOwnerDecision
                : TicketStatus.WaitingForTenantDecision;
            TransitionTo(next, "System");
        }

        public void StartWork()
        {
            if (Status != TicketStatus.VendorAccepted)
                throw new InvalidOperationException("Work can only start after vendor accepts.");
            TransitionTo(TicketStatus.InProgress, "Vendor");
        }

        public void CompleteWork()
        {
            if (Status != TicketStatus.InProgress)
                throw new InvalidOperationException("Work must be InProgress to complete.");

            if (ResolutionMethod == Enums.ResolutionMethod.PlatformVendor)
                TransitionTo(TicketStatus.AwaitingPayment, "Vendor");
            else
            {
                TransitionTo(TicketStatus.Completed, "System");
                ResolvedAt = DateTime.UtcNow;
            }
            MarkAsUpdated();
        }

        public void ConfirmPayment()
        {
            if (Status != TicketStatus.AwaitingPayment)
                throw new InvalidOperationException("Payment can only be confirmed when AwaitingPayment.");
            TransitionTo(TicketStatus.Completed, "System");
            ResolvedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }

        public void Cancel(string cancelledBy)
        {
            var cancellable = new[]
            {
            TicketStatus.Draft, TicketStatus.Submitted, TicketStatus.UnderAiReview,
            TicketStatus.AiEvaluated, TicketStatus.WaitingForOwnerDecision,
            TicketStatus.WaitingForTenantDecision
        };
            if (!cancellable.Contains(Status))
                throw new InvalidOperationException($"Cannot cancel a ticket in status {Status}.");
            TransitionTo(TicketStatus.Cancelled, cancelledBy);
        }

        public void SoftDelete()
        {
            if (Status != TicketStatus.Draft)
                throw new InvalidOperationException("Only Draft tickets can be deleted.");
            MarkAsDeleted();
        }

        private void TransitionTo(TicketStatus newStatus, string changedBy)
        {
            var previous = Status;
            Status = newStatus;
            MarkAsUpdated();
            StatusHistory.Add(TicketStatusHistory.Create(Id, previous, newStatus, changedBy));
            AddDomainEvent(new TicketStatusChangedDomainEvent(Id, previous, newStatus));
        }
    }
}