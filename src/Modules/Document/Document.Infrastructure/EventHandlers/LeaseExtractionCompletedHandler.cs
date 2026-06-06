using Document.Application.Events;
using Document.Domain.Enums;
using Document.Infrastructure.Persistence;
using MediatR;

namespace Document.Infrastructure.EventHandlers;

public class LeaseExtractionCompletedHandler : INotificationHandler<LeaseExtractionCompletedEvent>
{
    private readonly DocumentDbContext _db;

    public LeaseExtractionCompletedHandler(DocumentDbContext db) => _db = db;

    public async Task Handle(LeaseExtractionCompletedEvent evt, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { evt.LeaseId }, ct);
        if (lease == null) return;

        lease.RentAmount = evt.ExtractedData.RentAmount;
        lease.SecurityDeposit = evt.ExtractedData.SecurityDeposit;
        lease.StartDate = evt.ExtractedData.StartDate;
        lease.EndDate = evt.ExtractedData.EndDate;
        lease.PaymentDayOfMonth = evt.ExtractedData.PaymentDayOfMonth;
        lease.ExtractionStatus = LeaseExtractionStatus.Extracted;
        lease.ExtractionConfidence = evt.ExtractedData.Confidence;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}