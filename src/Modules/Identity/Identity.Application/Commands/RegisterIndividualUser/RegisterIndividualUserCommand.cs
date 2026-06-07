using MediatR;

namespace Identity.Application.Commands.RegisterIndividualUser;

public record RegisterIndividualUserCommand(
    string FullName,
    string Email,
    string Password,
    string Phone,
    string Role,
    string? NationalIdNumber = null
) : IRequest<Guid>;
