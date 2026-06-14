using MediatR;

namespace Identity.Application.Queries.GetMaintenanceCompaniesByService;

public record GetMaintenanceCompaniesByServiceQuery(string ServiceType) : IRequest<List<MaintenanceCompanyDto>>;