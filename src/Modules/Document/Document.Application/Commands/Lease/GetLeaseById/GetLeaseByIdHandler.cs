using Document.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Document.Application.Queries.Lease.GetLeaseById;

public class GetLeaseByIdHandler : IRequestHandler<GetLeaseByIdQuery, LeaseDto?>
{
    private readonly IDocumentDbContext _db;
    private readonly IAzureBlobService _blob;

    public GetLeaseByIdHandler(IDocumentDbContext db, IAzureBlobService blob)
    { _db = db; _blob = blob; }

    public async Task<LeaseDto?> Handle(GetLeaseByIdQuery query, CancellationToken ct)
    {
        var l = await _db.Leases
            .Where(x => x.Id == query.LeaseId && x.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (l == null) return null;

        var sasUrl = _blob.GenerateSasUrl(l.ContractFileUrl);

        return LeaseDto.FromEntity(l) with { ContractFileUrl = sasUrl };
    }
}