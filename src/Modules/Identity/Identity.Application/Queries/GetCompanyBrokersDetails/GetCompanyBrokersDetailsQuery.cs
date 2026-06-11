using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetCompanyBrokersDetails;

public record GetCompanyBrokersDetailsQuery(Guid CompanyId) : IRequest<List<BrokerDetailsDto>>;
