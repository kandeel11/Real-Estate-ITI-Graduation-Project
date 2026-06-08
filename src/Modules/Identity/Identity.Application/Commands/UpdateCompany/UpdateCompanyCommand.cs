using MediatR;

namespace Identity.Application.Commands.UpdateCompany;

public record UpdateCompanyCommand(
    Guid CompanyId,
    Guid RequestingUserId,
    string Name,
    string City,
    string Address,
    string Phone,
    string? Website,
    string? Description,
    string? LogoUrl
) : IRequest;
