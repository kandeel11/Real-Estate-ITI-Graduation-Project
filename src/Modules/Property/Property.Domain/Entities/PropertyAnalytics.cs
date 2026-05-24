using Domain.Entities;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class PropertyAnalytics : BaseEntity
{
    public Guid PropertyId { get; private set; }
    public DateTime ViewedAt { get; private set; }
    public Guid? ViewerId { get; private set; }
    public string? IpAddress { get; private set; }
    public ViewSource? Source { get; private set; }

    private PropertyAnalytics() { }

    public static PropertyAnalytics Create(
        Guid propertyId,
        Guid? viewerId,
        string? ipAddress,
        ViewSource? source)
    {
        return new PropertyAnalytics
        {
            PropertyId = propertyId,
            ViewedAt = DateTime.UtcNow,
            ViewerId = viewerId,
            IpAddress = ipAddress,
            Source = source
        };
    }
}
