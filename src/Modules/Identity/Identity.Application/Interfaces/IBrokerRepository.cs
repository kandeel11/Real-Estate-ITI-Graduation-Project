using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IBrokerRepository
{
    Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Broker?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<Broker>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default);
    Task AddAsync(Broker broker, CancellationToken ct = default);
    void Remove(Broker broker);
    Task SaveChangesAsync(CancellationToken ct = default);
}
