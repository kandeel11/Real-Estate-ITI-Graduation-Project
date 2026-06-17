using Identity.Application.Interfaces;
using MediatR;

namespace Identity.Application.Queries.GetMaintenanceCompaniesByService;

public class GetMaintenanceCompaniesByServiceHandler
    : IRequestHandler<GetMaintenanceCompaniesByServiceQuery, List<MaintenanceCompanyDto>>
{
    private readonly IMaintenanceCompanyRepository _repo;

    public GetMaintenanceCompaniesByServiceHandler(IMaintenanceCompanyRepository repo)
        => _repo = repo;

    public async Task<List<MaintenanceCompanyDto>> Handle(
        GetMaintenanceCompaniesByServiceQuery request,
        CancellationToken cancellationToken)
    {
        var companies = await _repo.GetByServiceTypeAsync(request.ServiceType, cancellationToken);

        return companies.Select(c => new MaintenanceCompanyDto(
            c.Id,
            c.CompanyName,
            c.GetServiceTypes(),
            c.Email,
            c.Phone,
            c.City,
            c.IsActive,
            c.RatingAvg,
            c.TotalRatings)).ToList();
    }
}