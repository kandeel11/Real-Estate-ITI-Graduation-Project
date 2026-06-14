using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public record RefreshTokenCommand(
    string Token,
    string? IpAddress
) : IRequest<AuthResponseDto>;
