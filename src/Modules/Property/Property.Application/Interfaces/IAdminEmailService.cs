namespace Property.Application.Interfaces;

public interface IAdminEmailService
{
    Task NotifyPropertySubmittedAsync(
        Guid propertyId,
        string title,
        Guid ownerId,
        string city,
        CancellationToken ct = default);
}
