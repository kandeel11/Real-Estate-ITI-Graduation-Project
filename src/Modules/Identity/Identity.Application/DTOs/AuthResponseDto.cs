namespace Identity.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    CurrentUserDto User);

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    bool IsEmailVerified);
