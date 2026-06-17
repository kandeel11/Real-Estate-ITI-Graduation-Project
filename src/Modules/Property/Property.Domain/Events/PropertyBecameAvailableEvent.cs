using MediatR;

namespace Property.Domain.Events;

/// <summary>
/// Raised when a rented property returns to the market (its lease ended). Handled
/// inside the Property module to restore the owner's/company's property usage slot
/// — the inverse of <see cref="PropertyRentedEvent"/>.
/// </summary>
public record PropertyBecameAvailableEvent(Guid PropertyId, Guid OwnerId, Guid? CompanyId) : INotification;
