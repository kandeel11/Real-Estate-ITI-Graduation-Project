using Document.Application.Commands.Lease.ConfirmLease;
using Document.Application.Interfaces;
using Document.Domain.Enums;
using Document.Domain.Events;
using MediatR;
namespace Document.Application.Commands.Lease.ConfirmLease;

public class ConfirmLeaseHandler : IRequestHandler<ConfirmLeaseCommand>
{
    private readonly IDocumentDbContext _db;
    private readonly IPublisher _publisher;

    public ConfirmLeaseHandler(IDocumentDbContext db, IPublisher publisher)
    { _db = db; _publisher = publisher; }

    public async Task Handle(ConfirmLeaseCommand cmd, CancellationToken ct)
    {
        var lease = await _db.Leases.FindAsync(new object[] { cmd.LeaseId }, ct)
            ?? throw new Exception("Lease not found");

        lease.RentAmount = cmd.RentAmount;
        lease.SecurityDeposit = cmd.SecurityDeposit;
        lease.StartDate = cmd.StartDate;
        lease.EndDate = cmd.EndDate;
        lease.PaymentDayOfMonth = cmd.PaymentDayOfMonth;
        lease.IsActive = true;
        lease.ExtractionStatus = LeaseExtractionStatus.Confirmed;
        lease.ConfirmedById = cmd.OwnerId;
        lease.ConfirmedAt = DateTime.UtcNow;
        lease.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _publisher.Publish(
            new LeaseActivatedEvent(lease.Id, lease.PropertyId, lease.TenantId, lease.OwnerId), ct);
    }
}