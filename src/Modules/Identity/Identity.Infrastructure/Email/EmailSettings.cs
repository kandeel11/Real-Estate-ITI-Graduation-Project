// Identity.Infrastructure/Email/EmailSettings.cs

namespace Identity.Infrastructure.Email;

public sealed class EmailSettings
{
    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = "Real Estate Platform";
    public bool UseSsl { get; set; } = true;
    public string AppBaseUrl { get; set; } = "https://localhost:5001";
}