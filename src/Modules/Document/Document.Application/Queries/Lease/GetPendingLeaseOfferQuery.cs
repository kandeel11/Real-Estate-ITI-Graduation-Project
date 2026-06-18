using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetPendingLeaseOfferQuery(Guid TenantId) : IRequest<LeaseDto?>;

public class GetPendingLeaseOfferHandler(IDocumentDbContext db)
    : IRequestHandler<GetPendingLeaseOfferQuery, LeaseDto?>
{
    public async Task<LeaseDto?> Handle(GetPendingLeaseOfferQuery q, CancellationToken ct)
    {
        var l = await db.Leases
            .Where(x =>
                x.TenantId == q.TenantId &&
                x.DeletedAt == null &&
                !x.IsActive &&
                x.ExtractionStatus == "extracted" &&
                x.TenantConfirmedAt == null &&
                x.RejectionReason == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (l is null) return null;

        return new LeaseDto(
            l.Id, l.PropertyId, l.TenantId, l.OwnerId,
            l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
            l.SecurityDeposit, l.StartDate, l.EndDate,
            l.PaymentDayOfMonth, l.ExtractionStatus,
            l.ExtractionConfidence, l.IsActive, l.ContractFileUrl, l.CreatedAt);
    }
}
