using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<User?> GetByVerificationTokenAsync(string token, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<Dictionary<Guid, string>> GetFullNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);
}
