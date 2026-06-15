using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Commands.ToggleMaintenanceCompany;

public class ToggleMaintenanceCompanyHandler : IRequestHandler<ToggleMaintenanceCompanyCommand, bool>
{
    private readonly IMaintenanceCompanyRepository _repo;

    public ToggleMaintenanceCompanyHandler(IMaintenanceCompanyRepository repo)
        => _repo = repo;

    public async Task<bool> Handle(ToggleMaintenanceCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repo.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Maintenance company not found.");

        if (company.IsActive)
            company.Deactivate();
        else
            company.Activate();

        await _repo.SaveChangesAsync(cancellationToken);

        return company.IsActive;
    }
}