using MediatR;

namespace Identity.Application.Queries.GetCompanyManagerId;

/// <summary>Returns the UserId of the company manager for the given company, or null if none found.</summary>
public record GetCompanyManagerIdQuery(Guid CompanyId) : IRequest<Guid?>;
