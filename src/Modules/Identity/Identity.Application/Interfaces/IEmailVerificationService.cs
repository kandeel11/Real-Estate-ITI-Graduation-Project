using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IEmailVerificationService
{
    Task SendVerificationEmailAsync(User user, CancellationToken ct);
    Task SendWelcomeEmailAsync(User user, CancellationToken ct);
}
