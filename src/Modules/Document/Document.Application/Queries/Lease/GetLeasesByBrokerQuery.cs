using Document.Application.Interfaces;
using Document.Application.Queries.Lease.GetLeaseById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetLeasesByBrokerQuery(Guid BrokerId) : IRequest<List<LeaseDto>>;

public class GetLeasesByBrokerHandler(IDocumentDbContext db)
    : IRequestHandler<GetLeasesByBrokerQuery, List<LeaseDto>>
{
    public async Task<List<LeaseDto>> Handle(GetLeasesByBrokerQuery q, CancellationToken ct)
    {
        var leases = await db.Leases
            .Where(l => l.BrokerId == q.BrokerId && l.DeletedAt == null)
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
