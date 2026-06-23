using Microsoft.EntityFrameworkCore;
using Property.Application.DTOs;
using Property.Application.Interfaces;
using Property.Domain.Entities;
using Property.Infrastructure.Persistence.Contexts;

namespace Property.Infrastructure.Persistence.Repositories;

public class SavedPropertyRepository : ISavedPropertyRepository
{
    private readonly PropertyDbContext _context;

    public SavedPropertyRepository(PropertyDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(Guid userId, Guid propertyId, CancellationToken ct = default)
        => _context.SavedProperties.AnyAsync(s => s.UserId == userId && s.PropertyId == propertyId, ct);

    public Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default)
        => _context.SavedProperties.CountAsync(s => s.UserId == userId, ct);

    public Task<SavedProperty?> GetAsync(Guid userId, Guid propertyId, CancellationToken ct = default)
        => _context.SavedProperties.FirstOrDefaultAsync(s => s.UserId == userId && s.PropertyId == propertyId, ct);

    public async Task AddAsync(SavedProperty saved, CancellationToken ct = default)
        => await _context.SavedProperties.AddAsync(saved, ct);

    public Task RemoveAsync(SavedProperty saved, CancellationToken ct = default)
    {
        _context.SavedProperties.Remove(saved);
        return Task.CompletedTask;
    }

    public Task<int> RemoveByPropertyAsync(Guid propertyId, CancellationToken ct = default)
        => _context.SavedProperties
            .Where(s => s.PropertyId == propertyId)
            .ExecuteDeleteAsync(ct);

    // Only ids whose property still exists (the Property query filter drops soft-deleted ones).
    public Task<List<Guid>> GetSavedIdsAsync(Guid userId, CancellationToken ct = default)
        => _context.SavedProperties
            .Where(s => s.UserId == userId)
            .Join(_context.Properties, s => s.PropertyId, p => p.Id, (s, p) => p.Id)
            .ToListAsync(ct);

    public Task<List<SavedPropertyDto>> GetSavedByUserAsync(Guid userId, CancellationToken ct = default)
        => _context.SavedProperties
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Join(_context.Properties, s => s.PropertyId, p => p.Id, (s, p) => new SavedPropertyDto
            {
                Id = p.Id,
                Title = p.Title,
                PropertyType = p.PropertyType.ToString(),
                Price = p.Price,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                AreaSqm = p.AreaSqm,
                City = p.City,
                District = p.District,
                IsFeatured = p.IsFeatured,
                PrimaryImageUrl = _context.PropertyMedia
                    .Where(m => m.PropertyId == p.Id && m.IsPrimary)
                    .Select(m => m.MediaUrl)
                    .FirstOrDefault(),
                SavedAt = s.CreatedAt
            })
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
