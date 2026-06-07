using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetBrokersByCompany;

public record GetBrokersByCompanyQuery(Guid CompanyId) : IRequest<List<BrokerDto>>;
