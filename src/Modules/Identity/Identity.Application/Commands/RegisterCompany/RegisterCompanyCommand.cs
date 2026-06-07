using MediatR;

namespace Identity.Application.Commands.RegisterCompany;

public record RegisterCompanyCommand(
    string AdminFullName,
    string AdminEmail,
    string Password,
    string AdminPhone,
    string CompanyName,
    string CommercialRegister,
    string City,
    string Address,
    string CompanyPhone,
    string CompanyEmail
) : IRequest<Guid>;
