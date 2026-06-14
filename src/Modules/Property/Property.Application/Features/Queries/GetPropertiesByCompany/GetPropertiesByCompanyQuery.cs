using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertiesByCompany;

public record GetPropertiesByCompanyQuery(Guid CompanyId) : IRequest<List<PropertySummaryDto>>;
