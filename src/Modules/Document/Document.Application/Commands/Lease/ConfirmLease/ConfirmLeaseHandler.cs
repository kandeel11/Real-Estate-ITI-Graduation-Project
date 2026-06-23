using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;
using PaymentFrequencyEnum = Document.Domain.Enums.PaymentFrequency;

namespace Document.Application.Commands.Lease.ConfirmLease;

public class ConfirmLeaseHandler : IRequestHandler<ConfirmLeaseCommand, ConfirmLeaseResult>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;

    public ConfirmLeaseHandler(IDocumentDbContext db, IPublisher publisher)
    { _db = db; _publisher = publisher; }

    public async Task<ConfirmLeaseResult> Handle(ConfirmLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        bool isOwner = cmd.ConfirmerId == lease.OwnerId;
        bool isTenant = cmd.ConfirmerId == lease.TenantId;

        if (!isOwner && !isTenant)
            throw new UnauthorizedAccessException("Only the owner or tenant can confirm this lease.");

        if (lease.ExtractionStatus == LeaseExtractionStatus.Rejected)
            throw new InvalidOperationException("This lease was rejected and cannot be confirmed.");

        if (isOwner)
        {
            lease.InstallmentAmount = cmd.InstallmentAmount;
            lease.SecurityDeposit   = cmd.SecurityDeposit;
            lease.StartDate         = cmd.StartDate;
            lease.EndDate           = cmd.EndDate;
            lease.PaymentDayOfMonth = cmd.PaymentDayOfMonth;
            lease.TotalRentAmount   = cmd.TotalRentAmount;
            lease.DurationInMonths  = cmd.DurationInMonths;
            if (cmd.PaymentFrequency != null &&
                Enum.TryParse<PaymentFrequencyEnum>(cmd.PaymentFrequency, ignoreCase: true, out var freq))
                lease.PaymentFrequency = freq;
            lease.OwnerConfirmedAt = DateTime.UtcNow;
        }
        else
        {
            lease.TenantConfirmedAt = DateTime.UtcNow;
        }

        lease.ConfirmedById = cmd.ConfirmerId;
        lease.ConfirmedAt = DateTime.UtcNow;
        lease.UpdatedAt = DateTime.UtcNow;

        bool bothConfirmed = lease.OwnerConfirmedAt.HasValue && lease.TenantConfirmedAt.HasValue;

        if (bothConfirmed && !lease.IsActive)
        {
            lease.IsActive = true;
            lease.ExtractionStatus = LeaseExtractionStatus.Confirmed;

            bool isRenewal = lease.RenewedFromLeaseId is { } prevId;

            // A renewal keeps the SAME tenant in the SAME unit, so the property stays
            // Rented and the usage slot stays consumed — supersede the predecessor
            // quietly (no LeaseEndedEvent) and skip the property-facing activation event.
            if (isRenewal)
            {
                var previous = await _db.Leases.FindAsync(new object[] { lease.RenewedFromLeaseId! }, ct);
                if (previous is not null && previous.IsActive)
                {
                    previous.IsActive = false;
                    previous.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                await _publisher.Publish(
                    new LeaseActivatedEvent(
                        lease.Id,
                        lease.PropertyId,
                        lease.OwnerId,
                        lease.InstallmentAmount ?? 0,
                        lease.PaymentDayOfMonth ?? 1,
                        lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                        lease.ContractFileUrl
                    ), ct);
            }

            await _publisher.Publish(
                new LeaseActivatedIntegrationEvent(
                    LeaseId: lease.Id,
                    PropertyId: lease.PropertyId,
                    OwnerId: lease.OwnerId,
                    TenantId: lease.TenantId,
                    InstallmentAmount: lease.InstallmentAmount ?? 0,
                    PaymentFrequency: lease.PaymentFrequency?.ToString() ?? "Monthly",
                    DurationInMonths: lease.DurationInMonths ?? 12,
                    PaymentDayOfMonth: lease.PaymentDayOfMonth ?? 1,
                    StartDate: lease.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    EndDate: lease.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
                ), ct);
        }

        await _db.SaveChangesAsync(ct);

        return new ConfirmLeaseResult(
            lease.IsActive,
            lease.OwnerConfirmedAt.HasValue,
            lease.TenantConfirmedAt.HasValue,
            lease.ExtractionStatus.ToString());
    }
}

public class RejectLeaseHandler : IRequestHandler<RejectLeaseCommand, RejectLeaseResult>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public RejectLeaseHandler(IDocumentDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<RejectLeaseResult> Handle(RejectLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        if (cmd.TenantId != lease.TenantId)
            throw new UnauthorizedAccessException("Only the tenant can reject this lease.");

        // The parties already agreed to the lease in real life, so a decline means the
        // uploaded contract has an error. Mark it rejected (it drops out of the tenant's
        // pending offer) but keep the lead WON so the broker/owner can recreate a corrected
        // lease — the rejected one no longer counts as "Lease Created" in the pipeline.
        lease.ExtractionStatus = LeaseExtractionStatus.Rejected;
        lease.RejectionReason = cmd.Reason;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Tell whoever created the lease (broker if assigned, else the owner) to fix & re-send it.
        var recreatorId = lease.BrokerId ?? lease.OwnerId;
        await _notifications.CreateInAppAsync(
            userId: recreatorId,
            title: "Lease Declined — Please Recreate",
            body: $"The tenant declined the lease — there may be an error in the contract. Please recreate it. Reason: {cmd.Reason}",
            type: NotificationType.LeaseRejected,
            relatedEntityType: "lease",
            relatedEntityId: lease.Id,
            ct: ct
        );

        return new RejectLeaseResult(true, "Rejected");
    }
}