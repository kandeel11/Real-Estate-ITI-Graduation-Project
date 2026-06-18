using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;

namespace Document.Application.Commands.Lease.UploadLeaseContract;

public class UploadLeaseContractHandler : IRequestHandler<UploadLeaseContractCommand, UploadLeaseResponse>
{
    private readonly IDocumentDbContext _db;
    private readonly IAzureBlobService _blob;
    private readonly IPublisher _publisher;

    public UploadLeaseContractHandler(IDocumentDbContext db, IAzureBlobService blob, IPublisher publisher)
    { _db = db; _blob = blob; _publisher = publisher; }

    public async Task<UploadLeaseResponse> Handle(UploadLeaseContractCommand cmd, CancellationToken ct)
    {
        if (cmd.File.Length > 10 * 1024 * 1024)
            throw new Exception("File size exceeds 10MB");

        var lead = await _db.Leads.FindAsync(new object[] { cmd.LeadId }, ct)
            ?? throw new KeyNotFoundException($"Lead {cmd.LeadId} not found");

        using var stream = cmd.File.OpenReadStream();
        var blobName  = await _blob.UploadAsync(stream, cmd.File.FileName);

        var lease = new Document.Domain.Entities.Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = lead.PropertyId,
            TenantId = lead.TenantId,
            // Derive the real owner from the lead — the controller no longer fabricates one.
            OwnerId = lead.OwnerId,
            LeadId = cmd.LeadId,
            ContractFileUrl = blobName,
            ExtractionStatus = LeaseExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Leases.Add(lease);
        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(new LeaseUploadedEvent(lease.Id, blobName, lease.OwnerId), ct);

        var updated = await _db.Leases.FindAsync(new object[] { lease.Id }, ct);

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