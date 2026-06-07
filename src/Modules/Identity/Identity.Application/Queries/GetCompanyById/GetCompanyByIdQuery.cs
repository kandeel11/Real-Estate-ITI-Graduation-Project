using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetCompanyById;

public record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanyDto?>;
