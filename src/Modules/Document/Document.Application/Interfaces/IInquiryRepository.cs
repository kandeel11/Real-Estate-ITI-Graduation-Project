using Document.Domain.Entities;

namespace Document.Application.Interfaces;

public interface IInquiryRepository
{
    Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Inquiry>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default);
    Task<List<Inquiry>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<List<Inquiry>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
    Task AddAsync(Inquiry inquiry, CancellationToken ct = default);
    Task UpdateAsync(Inquiry inquiry, CancellationToken ct = default);
}