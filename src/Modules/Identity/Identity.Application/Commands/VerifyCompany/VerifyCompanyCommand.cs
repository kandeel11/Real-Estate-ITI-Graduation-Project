using MediatR;

namespace Identity.Application.Commands.VerifyCompany;

public record VerifyCompanyCommand(Guid CompanyId) : IRequest;
