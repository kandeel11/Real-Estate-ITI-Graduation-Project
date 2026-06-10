using MaintenanceCompanies.Application.DTOs.Companies;
using MediatR;

namespace MaintenanceCompanies.Application.Commands.CreateCompany;

public record CreateMaintenanceCompanyCommand(
    string UserId,
    string AdminId,
    string CompanyName,
    string Email,
    string Phone,
    string City,
    int ServiceRadiusKm,
    string? Description,
    string? LogoUrl,
    List<Guid> CategoryIds
) : IRequest<MaintenanceCompanyResponseDto>;