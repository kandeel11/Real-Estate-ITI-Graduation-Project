// AI.Infrastructure/Persistence/Repositories/SavedSearchRepository.cs

using AI.Application.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI.Infrastructure.Persistence.Repositories;

public sealed class SavedSearchRepository : ISavedSearchRepository
{
    // ✅ Inject IAiDbContext — not AiDbContext directly (Clean Architecture rule)
    private readonly IAiDbContext _db;

    public SavedSearchRepository(IAiDbContext db)
        => _db = db;

    public async Task<SavedSearch?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => await _db.SavedSearches
                    .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<SavedSearch>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
        => await _db.SavedSearches
                    .Where(s => s.UserId == userId && s.IsActive)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync(ct);

    public async Task<IReadOnlyList<SavedSearch>> GetActiveWithNotificationsAsync(
        CancellationToken ct = default)
        => await _db.SavedSearches
                    .Where(s => s.IsActive && s.NotifyOnMatch)
                    .ToListAsync(ct);

    public async Task AddAsync(
        SavedSearch search, CancellationToken ct = default)
        => await _db.SavedSearches.AddAsync(search, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}