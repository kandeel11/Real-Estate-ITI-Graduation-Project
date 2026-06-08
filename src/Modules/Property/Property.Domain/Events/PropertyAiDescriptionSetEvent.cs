using MediatR;

namespace Property.Domain.Events;

public record PropertyAiDescriptionSetEvent(
    Guid PropertyId,
    Guid OwnerId,
    Guid? CompanyId
) : INotification;
