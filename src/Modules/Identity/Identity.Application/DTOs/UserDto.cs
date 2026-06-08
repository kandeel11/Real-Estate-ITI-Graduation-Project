namespace Identity.Application.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Phone,
    string Role,
    Guid? CompanyId,
    bool IsActive,
    bool IsEmailVerified);
