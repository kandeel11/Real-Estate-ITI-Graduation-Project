using MediatR;

namespace Identity.Application.Commands.RegisterMaintenanceCompany;

public record RegisterMaintenanceCompanyCommand(
    string CompanyName,
    string[] ServiceTypes,
    string Email,
    string Phone,
    string City,
    string FullName,
    string TempPassword
) : IRequest<Guid>;
