using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Lease.SendLeaseToTenant;

/// <summary>
/// Persists the broker's reviewed/edited lease terms and either:
///   – routes to the company manager for approval (when ManagerId is provided), or
///   – notifies the tenant directly (independent broker or owner flow).
/// </summary>
public record SendLeaseToTenantCommand(
    Guid LeaseId,
    decimal? InstallmentAmount,
    decimal? Deposit,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? PaymentFrequency = null,
    decimal? TotalRentAmount = null,
    int? DurationInMonths = null,
    int? PaymentDayOfMonth = null,
    Guid? ManagerId = null) : IRequest;

public class SendLeaseToTenantCommandHandler : IRequestHandler<SendLeaseToTenantCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly INotificationService _notifications;

    public SendLeaseToTenantCommandHandler(IDocumentDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task Handle(SendLeaseToTenantCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases
            .FirstOrDefaultAsync(l => l.Id == cmd.LeaseId && l.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.LeaseId} not found.");

        if (cmd.InstallmentAmount.HasValue) lease.InstallmentAmount = cmd.InstallmentAmount;
        if (cmd.Deposit.HasValue) lease.SecurityDeposit = cmd.Deposit;
        if (cmd.StartDate.HasValue) lease.StartDate = cmd.StartDate;
        if (cmd.EndDate.HasValue) lease.EndDate = cmd.EndDate;
        if (cmd.TotalRentAmount.HasValue) lease.TotalRentAmount = cmd.TotalRentAmount;
        if (cmd.DurationInMonths.HasValue) lease.DurationInMonths = cmd.DurationInMonths;
        if (cmd.PaymentDayOfMonth.HasValue && cmd.PaymentDayOfMonth.Value is >= 1 and <= 31)
            lease.PaymentDayOfMonth = cmd.PaymentDayOfMonth;
        if (!string.IsNullOrWhiteSpace(cmd.PaymentFrequency) &&
            Enum.TryParse<PaymentFrequency>(cmd.PaymentFrequency, ignoreCase: true, out var freq))
            lease.PaymentFrequency = freq;

        lease.RejectionReason = null;
        lease.UpdatedAt = DateTime.UtcNow;

        if (cmd.ManagerId.HasValue)
        {
            // Company broker path: route to manager for approval before tenant sees the lease.
            lease.ManagerApprovalStatus = "PendingApproval";
            lease.ManagerId = cmd.ManagerId;

            await _db.SaveChangesAsync(ct);

            await _notifications.CreateInAppAsync(
                userId: cmd.ManagerId.Value,
                title: "Lease Awaiting Your Approval",
                body: "A broker has uploaded a lease that requires your approval before it is sent to the tenant.",
                type: NotificationType.ContractSigned,
                relatedEntityType: "lease",
                relatedEntityId: lease.Id,
                ct: ct);
            return;
        }

        // Owner / independent-broker path: send directly to tenant.
        lease.OwnerConfirmedAt ??= DateTime.UtcNow;
        lease.ExtractionStatus = LeaseExtractionStatus.Extracted;

        // If this is a renewal being offered, clear the predecessor's pending renewal request.
        if (lease.RenewedFromLeaseId is { } prevId)
        {
            var predecessor = await _db.Leases
                .FirstOrDefaultAsync(l => l.Id == prevId && l.DeletedAt == null, ct);
            if (predecessor?.RenewalRequestedAt is not null)
            {
                predecessor.RenewalRequestedAt = null;
                predecessor.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        await _notifications.CreateInAppAsync(
            userId: lease.TenantId,
            title: "Lease Ready for Review",
            body: "Your landlord has sent a lease agreement for your review and signature.",
            type: NotificationType.ContractSigned,
            relatedEntityType: "lease",
            relatedEntityId: lease.Id,
            ct: ct);
    }
}
