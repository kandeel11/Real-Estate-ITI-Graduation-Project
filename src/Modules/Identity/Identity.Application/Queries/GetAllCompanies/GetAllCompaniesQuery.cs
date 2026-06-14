using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetAllCompanies;

public record GetAllCompaniesQuery : IRequest<List<CompanyDetailsDto>>;
