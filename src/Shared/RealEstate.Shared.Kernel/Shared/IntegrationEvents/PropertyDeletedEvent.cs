using MediatR;

namespace RealEstate.Shared.Kernel.Shared.IntegrationEvents;

/// <summary>
/// Published by the Property module when a property is deleted, so other modules can
/// clean up their own references — currently the AI module removes the property's
/// vector from the Qdrant "properties" collection used for duplicate detection.
/// </summary>
public record PropertyDeletedEvent(Guid PropertyId) : INotification;
