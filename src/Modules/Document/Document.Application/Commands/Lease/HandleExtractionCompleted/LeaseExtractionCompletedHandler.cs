using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;

namespace Document.Application.Commands.Lease.HandleExtractionCompleted;

public class LeaseExtractionCompletedHandler : INotificationHandler<LeaseExtractionCompletedEvent>
{
    private readonly IDocumentDbContext _db;
    public LeaseExtractionCompletedHandler(IDocumentDbContext db) => _db = db;

    public async Task Handle(LeaseExtractionCompletedEvent evt, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { evt.LeaseId }, ct);
        if (lease == null) return;

        lease.RentAmount = evt.RentAmount;
        lease.SecurityDeposit = evt.SecurityDeposit;
        lease.StartDate = evt.StartDate;
        lease.EndDate = evt.EndDate;
        lease.PaymentDayOfMonth = evt.PaymentDayOfMonth;
        lease.ExtractionStatus = LeaseExtractionStatus.Extracted;
        lease.ExtractionConfidence = evt.Confidence;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}