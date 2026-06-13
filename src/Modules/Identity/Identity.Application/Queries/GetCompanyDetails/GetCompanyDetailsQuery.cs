using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetCompanyDetails;

public record GetCompanyDetailsQuery(Guid CompanyId) : IRequest<CompanyDetailsDto?>;
