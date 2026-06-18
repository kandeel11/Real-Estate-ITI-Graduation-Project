using Identity.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Property.Application.Interfaces;

namespace RealEstate.Host.Adapters;

public sealed class PropertyAdminEmailAdapter : IAdminEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly string _adminEmail;

    public PropertyAdminEmailAdapter(IEmailSender emailSender, IConfiguration config)
    {
        _emailSender = emailSender;
        _adminEmail = config["AdminEmail"] ?? "admin@realestate.app";
    }

    public async Task NotifyPropertySubmittedAsync(
        Guid propertyId, string title, Guid ownerId, string city, CancellationToken ct = default)
    {
        await _emailSender.SendAsync(
            toEmail: _adminEmail,
            toName: "Admin",
            subject: $"[Review Required] New property submitted: {title}",
            htmlBody: $"<p>A new property has been submitted for review.</p>" +
                      $"<ul>" +
                      $"<li><strong>Property ID:</strong> {propertyId}</li>" +
                      $"<li><strong>Title:</strong> {title}</li>" +
                      $"<li><strong>City:</strong> {city}</li>" +
                      $"<li><strong>Owner ID:</strong> {ownerId}</li>" +
                      $"</ul>" +
                      $"<p>Review at: GET /api/properties/pending-verification</p>",
            ct: ct);
    }
}
