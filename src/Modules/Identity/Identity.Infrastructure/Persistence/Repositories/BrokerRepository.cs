using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class BrokerRepository : IBrokerRepository
{
    private readonly IdentityDbContext _context;

    public BrokerRepository(IdentityDbContext context) => _context = context;

    public async Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Brokers.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Broker?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.Brokers.FirstOrDefaultAsync(b => b.UserId == userId, ct);

    public async Task<List<Broker>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default) =>
        await _context.Brokers.Where(b => b.CompanyId == companyId).ToListAsync(ct);

    public async Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default) =>
        await _context.Brokers.AnyAsync(b => b.LicenseNumber == licenseNumber.Trim(), ct);

    public async Task AddAsync(Broker broker, CancellationToken ct = default) =>
        await _context.Brokers.AddAsync(broker, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
