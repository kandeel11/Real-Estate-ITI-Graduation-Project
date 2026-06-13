using MaintenanceCompanies.Domain.Entities;

namespace MaintenanceCompanies.Application.Interfaces;

public interface ICurrentCompanyAccessor
{
    // بترجع الشركة لو موجودة، أو null لو مش موجودة
    Task<MaintenanceCompany?> GetCurrentCompanyAsync(CancellationToken ct = default);

    // بترجع الشركة، ولو مش موجودة بتضرب Exception فوراً (ودي اللي بنستخدمها دايماً)
    Task<MaintenanceCompany> RequireCurrentCompanyAsync(CancellationToken ct = default);
}