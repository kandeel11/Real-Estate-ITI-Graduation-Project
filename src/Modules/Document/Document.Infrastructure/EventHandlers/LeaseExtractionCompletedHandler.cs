using AI.Application.Events;
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

        lease.RentAmount = decimal.TryParse(evt.ExtractedData.RentAmount.Value, out var rent) ? rent : null;
        lease.SecurityDeposit = decimal.TryParse(evt.ExtractedData.SecurityDeposit.Value, out var dep) ? dep : null;
        lease.StartDate = DateOnly.TryParse(evt.ExtractedData.StartDate.Value, out var start) ? start : null;
        lease.EndDate = DateOnly.TryParse(evt.ExtractedData.EndDate.Value, out var end) ? end : null;
        lease.PaymentDayOfMonth = int.TryParse(evt.ExtractedData.PaymentDayOfMonth.Value, out var day) ? day : null;
        lease.ExtractionStatus = LeaseExtractionStatus.Extracted;
        lease.ExtractionConfidence = evt.ExtractedData.OverallConfidence;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}