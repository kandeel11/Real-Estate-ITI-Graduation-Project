using Identity.Application.DTOs;
using MediatR;

namespace Identity.Application.Queries.GetUsers;

/// <summary>Paged user directory for the admin dashboard.</summary>
public record PagedUsersDto(List<AdminUserDto> Items, int Total, int Page, int PageSize);

/// <summary>UserDto + moderation fields the admin needs.</summary>
public record AdminUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Phone,
    string Role,
    Guid? CompanyId,
    bool IsActive,
    bool IsEmailVerified,
    string? BanReason,
    DateTime? BannedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Role = null)
    : IRequest<PagedUsersDto>;
