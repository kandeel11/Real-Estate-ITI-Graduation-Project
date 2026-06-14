using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto?>;
