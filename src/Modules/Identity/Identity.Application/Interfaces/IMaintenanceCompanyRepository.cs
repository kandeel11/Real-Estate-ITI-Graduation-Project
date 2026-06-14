using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IMaintenanceCompanyRepository
{
    Task<MaintenanceCompany?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<MaintenanceCompany?> GetByUserIdAsync(Guid userId, CancellationToken ct = default); 
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(MaintenanceCompany company, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}