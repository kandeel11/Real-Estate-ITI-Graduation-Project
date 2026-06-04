using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Document.Application.Commands.Lease;

public record UploadLeaseContractCommand(
    Guid LeadId,
    Guid OwnerId,
    IFormFile File) : IRequest<Guid>;

public class UploadLeaseContractHandler : IRequestHandler<UploadLeaseContractCommand, Guid>
{
    private readonly IDocumentDbContext _db;
    private readonly IAzureBlobService _blob;
    private readonly ILeaseExtractionService _extraction;

    public UploadLeaseContractHandler(
        IDocumentDbContext db,
        IAzureBlobService blob,
        ILeaseExtractionService extraction)
    {
        _db = db;
        _blob = blob;
        _extraction = extraction;
    }

    public async Task<Guid> Handle(UploadLeaseContractCommand cmd, CancellationToken ct)
    {
        if (cmd.File.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 10MB");

        var lead = await _db.Leads.FindAsync(new object[] { cmd.LeadId }, ct)
            ?? throw new InvalidOperationException("Lead not found");

        // 1. رفع الـ PDF على Azure Blob
        using var uploadStream = cmd.File.OpenReadStream();
        var fileUrl = await _blob.UploadAsync(uploadStream, cmd.File.FileName);

        // 2. إنشاء الـ Lease record
        var lease = new Domain.Entities.Lease
        {
            Id = Guid.NewGuid(),
            PropertyId = lead.PropertyId,
            TenantId = lead.TenantId,
            OwnerId = cmd.OwnerId,
            LeadId = cmd.LeadId,
            ContractFileUrl = fileUrl,
            ExtractionStatus = LeaseExtractionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Leases.Add(lease);
        await _db.SaveChangesAsync(ct);

        // 3. بعت الـ PDF لـ Mohamed's service — بتاخد byte[] مش Stream
        try
        {
            using var ms = new MemoryStream();
            using var readStream = cmd.File.OpenReadStream();
            await readStream.CopyToAsync(ms, ct);
            var pdfBytes = ms.ToArray();

            var result = await _extraction.ExtractAsync(pdfBytes, ct);

            lease.RentAmount = decimal.TryParse(result.RentAmount?.Value, out var rent) ? rent : null;
            lease.SecurityDeposit = decimal.TryParse(result.SecurityDeposit?.Value, out var dep) ? dep : null;
            lease.StartDate = DateOnly.TryParse(result.StartDate?.Value, out var start) ? start : null;
            lease.EndDate = DateOnly.TryParse(result.EndDate?.Value, out var end) ? end : null;
            lease.PaymentDayOfMonth = int.TryParse(result.PaymentDayOfMonth?.Value, out var day) ? day : null;
            lease.ExtractionStatus = LeaseExtractionStatus.Extracted;
            lease.ExtractionConfidence = result.OverallConfidence;
            lease.UpdatedAt = DateTime.UtcNow;
        }
        catch
        {
            lease.ExtractionStatus = LeaseExtractionStatus.Failed;
        }

        await _db.SaveChangesAsync(ct);
        return lease.Id;
    }
}