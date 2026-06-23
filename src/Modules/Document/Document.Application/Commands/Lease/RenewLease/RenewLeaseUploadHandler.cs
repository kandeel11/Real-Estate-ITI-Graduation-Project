using Document.Application.Commands.Lease;
using Document.Application.Commands.Lease.UploadLeaseContract;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Commands.Lease.RenewLease;

public class RenewLeaseUploadHandler : IRequestHandler<RenewLeaseUploadCommand, UploadLeaseResponse>
{
    private readonly IDocumentDbContext _db;
    private readonly IAzureBlobService _blob;
    private readonly IPublisher _publisher;

    public RenewLeaseUploadHandler(IDocumentDbContext db, IAzureBlobService blob, IPublisher publisher)
    {
        _db = db;
        _blob = blob;
        _publisher = publisher;
    }

    public async Task<UploadLeaseResponse> Handle(RenewLeaseUploadCommand cmd, CancellationToken ct)
    {
        if (cmd.File.Length > 10 * 1024 * 1024)
            throw new Exception("File size exceeds 10MB");

        var previous = await _db.Leases.FindAsync(new object[] { cmd.PreviousLeaseId }, ct)
            ?? throw new KeyNotFoundException($"Lease {cmd.PreviousLeaseId} not found");

        if (!LeaseParties.IsLandlordSide(previous, cmd.RequesterId))
            throw new UnauthorizedAccessException("Only the owner or broker can renew this lease.");

        using var stream = cmd.File.OpenReadStream();
        var blobName = await _blob.UploadAsync(stream, cmd.File.FileName);

        // No-duplicate "resume": if an in-progress renewal draft already exists for this
        // predecessor (not yet active, not rejected), supersede it with a soft-delete, then
        // create a FRESH lease exactly like a first-time upload. Reusing the old lease id and
        // re-publishing LeaseUploadedEvent over its existing extraction state is fragile;
        // a clean new lease runs the same creation cycle that already works.
        var staleDrafts = await _db.Leases
            .Where(l => l.RenewedFromLeaseId == previous.Id
                        && l.DeletedAt == null
                        && !l.IsActive
                        && l.ExtractionStatus != LeaseExtractionStatus.Rejected)
            .ToListAsync(ct);
        foreach (var d in staleDrafts)
        {
            d.DeletedAt = DateTime.UtcNow;
            d.UpdatedAt = DateTime.UtcNow;
        }

        var renewal = new Document.Domain.Entities.Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = previous.PropertyId,
            TenantId = previous.TenantId,
            OwnerId = previous.OwnerId,
            BrokerId = previous.BrokerId,
            LeadId = previous.LeadId,
            InquiryId = previous.InquiryId,
            RenewedFromLeaseId = previous.Id,
            ContractFileUrl = blobName,
            ExtractionStatus = LeaseExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Leases.Add(renewal);

        // NOTE: the predecessor's RenewalRequestedAt is intentionally NOT cleared here.
        // It's cleared only when the renewal is actually sent to the tenant
        // (SendLeaseToTenantCommandHandler), so the request survives a mid-flow reload.

        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(new LeaseUploadedEvent(renewal.Id, blobName, renewal.OwnerId), ct);

        var updated = await _db.Leases.FindAsync(new object[] { renewal.Id }, ct);

        return new UploadLeaseResponse(
            updated!.Id,
            updated.ExtractionStatus,
            updated.InstallmentAmount == null ? null : new ExtractedLeaseData(
                updated.InstallmentAmount,
                updated.PaymentFrequency?.ToString(),
                updated.TotalRentAmount,
                updated.DurationInMonths,
                updated.SecurityDeposit,
                updated.StartDate,
                updated.EndDate,
                updated.PaymentDayOfMonth,
                updated.ExtractionConfidence));
    }
}
