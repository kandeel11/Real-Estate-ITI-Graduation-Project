using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetLeasesByPropertyIdsQuery(List<Guid> PropertyIds) : IRequest<List<LeaseDto>>;

public class GetLeasesByPropertyIdsHandler(IDocumentDbContext db)
    : IRequestHandler<GetLeasesByPropertyIdsQuery, List<LeaseDto>>
{
    public async Task<List<LeaseDto>> Handle(GetLeasesByPropertyIdsQuery q, CancellationToken ct)
    {
        if (q.PropertyIds.Count == 0) return [];

        var leases = await db.Leases
            .Where(l => q.PropertyIds.Contains(l.PropertyId) && l.DeletedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        return leases.Select(l => new LeaseDto(
            l.Id, l.PropertyId, l.TenantId, l.OwnerId,
            l.InstallmentAmount, l.PaymentFrequency?.ToString(), l.TotalRentAmount, l.DurationInMonths,
            l.SecurityDeposit, l.StartDate, l.EndDate,
            l.PaymentDayOfMonth, l.ExtractionStatus,
            l.ExtractionConfidence, l.IsActive, l.ContractFileUrl, l.CreatedAt)).ToList();
    }
}
