using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly IdentityDbContext _context;

    public CompanyRepository(IdentityDbContext context) => _context = context;

    public async Task<List<Company>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Companies.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Company?> GetByManagerIdAsync(Guid managerId, CancellationToken ct = default) =>
        await _context.Companies.FirstOrDefaultAsync(c => c.ManagerId == managerId, ct);

    public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await _context.Companies
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

    public async Task<bool> ExistsByCommercialRegisterAsync(string commercialRegister, CancellationToken ct = default) =>
        await _context.Companies.AnyAsync(
            c => c.CommercialRegister == commercialRegister.Trim(), ct);

    public async Task AddAsync(Company company, CancellationToken ct = default) =>
        await _context.Companies.AddAsync(company, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
