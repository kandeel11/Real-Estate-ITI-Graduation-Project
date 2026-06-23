using Domain.Entities;

namespace Property.Domain.Entities;

/// <summary>
/// A tenant's wishlist entry — links a user to a property they've saved.
/// One row per (UserId, PropertyId); the pair is unique. CreatedAt (from
/// BaseEntity) doubles as the "saved at" timestamp.
/// </summary>
public class SavedProperty : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid PropertyId { get; private set; }

    private SavedProperty() { }

    public static SavedProperty Create(Guid userId, Guid propertyId)
        => new() { UserId = userId, PropertyId = propertyId };
}
