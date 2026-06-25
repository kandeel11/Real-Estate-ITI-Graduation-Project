using MediatR;

namespace Maintenance.Domain.Events;

/// <summary>
/// Raised when the AI determines the landlord is responsible AND the property
/// is under full company management, triggering automatic routing to that company.
/// </summary>
public sealed record TicketRoutedToCompanyDomainEvent(
    Guid TicketId,
    Guid OriginalOwnerId,
    Guid RoutedToCompanyId,
    Guid CompanyFacingTicketId) : INotification;
