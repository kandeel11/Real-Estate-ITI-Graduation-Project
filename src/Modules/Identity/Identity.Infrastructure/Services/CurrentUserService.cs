using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // بندور في NameIdentifier الأول (بتاع ASP.NET)، وبعدين الـ Sub
            var idClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email) ??
        User?.FindFirstValue(JwtRegisteredClaimNames.Email);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? CompanyId
    {
        get
        {
            var c = User?.FindFirstValue("company_id");
            return Guid.TryParse(c, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    // From the "email_verified" JWT claim issued by JwtService.
    public bool IsEmailVerified =>
        string.Equals(User?.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}