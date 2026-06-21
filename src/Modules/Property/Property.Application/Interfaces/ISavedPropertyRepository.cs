using Property.Application.DTOs;
using Property.Domain.Entities;

namespace Property.Application.Interfaces;

public interface ISavedPropertyRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid propertyId, CancellationToken ct = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SavedProperty?> GetAsync(Guid userId, Guid propertyId, CancellationToken ct = default);
    Task AddAsync(SavedProperty saved, CancellationToken ct = default);
    Task RemoveAsync(SavedProperty saved, CancellationToken ct = default);

    /// <summary>Removes every tenant's wishlist entry for a property (called when it's deleted).</summary>
    Task<int> RemoveByPropertyAsync(Guid propertyId, CancellationToken ct = default);

    /// <summary>Ids the tenant has saved — lets the client mark cards without fetching details.</summary>
    Task<List<Guid>> GetSavedIdsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Saved listings joined to their (non-deleted) property, newest-saved first.</summary>
    Task<List<SavedPropertyDto>> GetSavedByUserAsync(Guid userId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
