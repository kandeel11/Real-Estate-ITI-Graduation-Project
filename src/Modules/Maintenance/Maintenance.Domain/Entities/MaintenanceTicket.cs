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

        // ── Company Management Routing ────────────────────────────────────────
        /// <summary>
        /// Set on the original tenant ticket when it is automatically routed to a
        /// management company (AI said Owner pays + property is full-management).
        /// </summary>
        public Guid? RoutedToCompanyId { get; private set; }

        /// <summary>
        /// Set on the company-facing ticket generated at routing time.
        /// References the original tenant ticket that triggered routing.
        /// </summary>
        public Guid? ParentTicketId { get; private set; }

        public string? TicketNumber { get; private set; } = default!;
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

        // ── Dispute Tracking ──────────────────────────────────────────────────
        /// <summary>The reason the party disputes the AI's responsibility decision.</summary>
        public string? DisputeReason { get; private set; }
        /// <summary>The user ID of whoever raised the dispute.</summary>
        public Guid? DisputedById { get; private set; }
        /// <summary>Role of the disputing party: "Tenant", "Owner", or "Company".</summary>
        public string? DisputedByRole { get; private set; }
        /// <summary>UTC timestamp when the dispute was raised.</summary>
        public DateTime? DisputeDate { get; private set; }
        /// <summary>Admin's notes recorded when resolving the dispute.</summary>
        public string? AdminNotes { get; private set; }

        public ICollection<MaintenanceTicketAttachment> Attachments { get; private set; } = new List<MaintenanceTicketAttachment>();
        public ICollection<MaintenanceTicketComment> Comments { get; private set; } = new List<MaintenanceTicketComment>();
        public ICollection<TicketStatusHistory> StatusHistory { get; private set; } = new List<TicketStatusHistory>();
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

        // ── Dispute Workflow ──────────────────────────────────────────────────

        /// <summary>
        /// Raises a dispute against the AI's responsibility decision.
        /// Only valid when the ticket is waiting for an Owner or Tenant decision.
        /// </summary>
        public void RaiseDispute(Guid disputedById, string disputedByRole, string reason)
        {
            if (Status != TicketStatus.WaitingForOwnerDecision
                && Status != TicketStatus.WaitingForTenantDecision)
                throw new InvalidOperationException(
                    $"Disputes can only be raised while a decision is pending. Current status: {Status}.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A dispute reason is required.", nameof(reason));

            DisputedById   = disputedById;
            DisputedByRole = disputedByRole;
            DisputeReason  = reason;
            DisputeDate    = DateTime.UtcNow;

            TransitionTo(TicketStatus.Disputed, disputedById.ToString());
            AddDomainEvent(new Events.TicketDisputeRaisedDomainEvent(Id, disputedById, disputedByRole, reason));
        }

        /// <summary>
        /// Admin resolves the dispute by declaring the final responsible party.
        /// The ticket transitions back to the appropriate decision-waiting state so
        /// the normal vendor-assignment flow can continue.
        /// </summary>
        public void ResolveDispute(string finalResponsibleParty, string? adminNotes)
        {
            if (Status != TicketStatus.Disputed)
                throw new InvalidOperationException("Only Disputed tickets can be resolved.");

            if (!Enum.TryParse<ResponsibleParty>(finalResponsibleParty, ignoreCase: true, out var parsed))
                throw new ArgumentException(
                    $"Invalid responsible party '{finalResponsibleParty}'. Must be 'Owner' or 'Tenant'.",
                    nameof(finalResponsibleParty));

            ResponsibleParty = parsed;
            AdminNotes       = adminNotes;

            var nextStatus = parsed == Enums.ResponsibleParty.Owner
                ? TicketStatus.WaitingForOwnerDecision
                : TicketStatus.WaitingForTenantDecision;

            TransitionTo(nextStatus, "Admin");
            AddDomainEvent(new Events.TicketDisputeResolvedDomainEvent(Id, parsed.ToString(), adminNotes));
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

        // ── Company Routing ───────────────────────────────────────────────────

        /// <summary>
        /// Marks the original tenant ticket as routed to a management company.
        /// Called after AI determines Owner is responsible and the property is
        /// under IsFullManagement = true.
        /// </summary>
        public void RouteToManagementCompany(Guid companyId, Guid companyFacingTicketId)
        {
            RoutedToCompanyId = companyId;
            MarkAsUpdated();
            AddDomainEvent(new Events.TicketRoutedToCompanyDomainEvent(
                Id, OwnerId, companyId, companyFacingTicketId));
        }

        /// <summary>
        /// Creates a company-facing shadow ticket that starts at WaitingForOwnerDecision.
        /// The company manager (acting as owner/landlord) sees this ticket in their queue
        /// and decides how to resolve it (vendor vs external technician).
        /// </summary>
        public static MaintenanceTicket CreateForCompany(
            Guid propertyId,
            Guid tenantId,
            Guid companyManagerUserId,
            string title,
            string description,
            TicketPriority priority,
            Guid parentTicketId,
            Guid routedToCompanyId,
            Guid? leaseId = null)
        {
            var ticket = new MaintenanceTicket
            {
                PropertyId        = propertyId,
                TenantId          = tenantId,
                OwnerId           = companyManagerUserId,
                LeaseId           = leaseId,
                Title             = $"[Company] {title}",
                Description       = description,
                Priority          = priority,
                Status            = TicketStatus.WaitingForOwnerDecision,
                ResponsibleParty  = Enums.ResponsibleParty.Owner,
                ParentTicketId    = parentTicketId,
                RoutedToCompanyId = routedToCompanyId,
            };

            // Seed the audit trail so the company ticket has a meaningful history
            ticket.StatusHistory.Add(TicketStatusHistory.Create(
                ticket.Id,
                TicketStatus.Draft,
                TicketStatus.WaitingForOwnerDecision,
                "System [Auto-routed from tenant ticket]"));

            return ticket;
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