using MediatR;

namespace Identity.Application.Commands.AddBroker;

public record AddBrokerCommand(
    Guid CompanyId,
    string FullName,
    string Email,
    string TempPassword,
    string Phone,
    string LicenseNumber
) : IRequest<Guid>;
