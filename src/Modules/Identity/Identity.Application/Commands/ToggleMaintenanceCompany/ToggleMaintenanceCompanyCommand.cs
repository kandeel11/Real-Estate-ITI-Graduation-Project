using MediatR;

namespace Identity.Application.Commands.ToggleMaintenanceCompany;

public record ToggleMaintenanceCompanyCommand(Guid CompanyId) : IRequest<bool>;