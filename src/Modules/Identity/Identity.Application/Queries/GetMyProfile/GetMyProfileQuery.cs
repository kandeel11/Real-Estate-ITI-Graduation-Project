using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetMyProfile;

/// <summary>Full profile (incl. phone) of the calling user — any role.</summary>
public record GetMyProfileQuery(Guid UserId) : IRequest<UserDto?>;
