using MediatR;

namespace Maintenance.Domain.Events;

public record VendorAssignedDomainEvent(
    Guid TicketId,
    Guid VendorId,
    Guid AssignmentId) : INotification;