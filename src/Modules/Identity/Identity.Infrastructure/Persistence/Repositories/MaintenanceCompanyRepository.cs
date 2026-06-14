using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class MaintenanceCompanyRepository : IMaintenanceCompanyRepository
{
    private readonly IdentityDbContext _context;

    public MaintenanceCompanyRepository(IdentityDbContext context) => _context = context;

    public async Task<MaintenanceCompany?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.MaintenanceCompanies.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<MaintenanceCompany?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.MaintenanceCompanies.FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.MaintenanceCompanies.AnyAsync(m => m.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task AddAsync(MaintenanceCompany company, CancellationToken ct = default) =>
        await _context.MaintenanceCompanies.AddAsync(company, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}