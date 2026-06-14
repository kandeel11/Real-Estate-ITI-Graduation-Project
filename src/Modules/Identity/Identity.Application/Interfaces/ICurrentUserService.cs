namespace Identity.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? CompanyId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
