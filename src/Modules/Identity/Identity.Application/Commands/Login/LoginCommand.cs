using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress
) : IRequest<AuthResponseDto>;
