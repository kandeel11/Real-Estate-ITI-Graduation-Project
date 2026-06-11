using MediatR;

namespace Identity.Application.Commands.DeleteBroker;

/// <summary>
/// Permanently removes a broker from the company and blocks their login.
/// The broker's PROPERTIES are intentionally untouched — they keep their
/// ListingBrokerId until the manager reassigns them via
/// PATCH /api/properties/{id}/assign-broker.
/// </summary>
public record DeleteBrokerCommand(
    Guid BrokerId,
    Guid? RequestingCompanyId,
    bool IsAdmin) : IRequest;
