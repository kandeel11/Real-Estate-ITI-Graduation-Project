using Identity.Application.Queries.GetCompanyManagerId;
using Identity.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Queries;

public sealed class GetCompanyManagerIdHandler
    : IRequestHandler<GetCompanyManagerIdQuery, Guid?>
{
    private readonly IdentityDbContext _db;

    public GetCompanyManagerIdHandler(IdentityDbContext db) => _db = db;

    public Task<Guid?> Handle(GetCompanyManagerIdQuery request, CancellationToken ct)
        => _db.Users
            .Where(u => u.CompanyId == request.CompanyId && u.Role == "company_manager")
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
}
