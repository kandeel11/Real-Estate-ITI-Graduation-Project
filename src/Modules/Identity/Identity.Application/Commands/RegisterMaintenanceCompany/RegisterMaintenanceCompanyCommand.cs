using MediatR;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public record RegisterMaintenanceCompanyCommand(
    string FullName,
    string Email,
    string Phone,
    string TempPassword
) : IRequest<Guid>;