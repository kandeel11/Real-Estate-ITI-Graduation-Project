using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface ICompanyRepository
{
    Task<List<Company>> GetAllAsync(CancellationToken ct = default);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Company?> GetByManagerIdAsync(Guid managerId, CancellationToken ct = default);
    Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<bool> ExistsByCommercialRegisterAsync(string commercialRegister, CancellationToken ct = default);
    Task AddAsync(Company company, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
