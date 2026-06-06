// AI.Application/Interfaces/ISavedSearchRepository.cs

using AI.Domain.Entities;

namespace AI.Application.Interfaces;

public interface ISavedSearchRepository
{
    Task<SavedSearch?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearch>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SavedSearch>> GetActiveWithNotificationsAsync(CancellationToken ct = default);
    Task AddAsync(SavedSearch search, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}