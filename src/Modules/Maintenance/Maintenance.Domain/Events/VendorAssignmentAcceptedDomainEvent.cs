using MediatR;

namespace Maintenance.Domain.Events;

public record VendorAssignmentAcceptedDomainEvent(
    Guid TicketId,
    Guid VendorId) : INotification;