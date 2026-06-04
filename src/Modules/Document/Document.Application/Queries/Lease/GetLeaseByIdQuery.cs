using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease;

public record GetLeaseByIdQuery(Guid LeaseId) : IRequest<LeaseDto?>;

public record LeaseDto(
    Guid Id, Guid PropertyId, Guid TenantId, Guid OwnerId,
    decimal? RentAmount, decimal? SecurityDeposit,
    DateOnly? StartDate, DateOnly? EndDate,
    int? PaymentDayOfMonth, string ExtractionStatus,
    decimal? ExtractionConfidence, bool IsActive,
    string ContractFileUrl);

public class GetLeaseByIdHandler : IRequestHandler<GetLeaseByIdQuery, LeaseDto?>
{
    private readonly IDocumentDbContext _db;
    public GetLeaseByIdHandler(IDocumentDbContext db) => _db = db;

    public async Task<LeaseDto?> Handle(GetLeaseByIdQuery query, CancellationToken ct)
    {
        var l = await _db.Leases
            .Where(x => x.Id == query.LeaseId && x.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (l == null) return null;

        return new LeaseDto(l.Id, l.PropertyId, l.TenantId, l.OwnerId,
            l.RentAmount, l.SecurityDeposit, l.StartDate, l.EndDate,
            l.PaymentDayOfMonth, l.ExtractionStatus,
            l.ExtractionConfidence, l.IsActive, l.ContractFileUrl);
    }
}