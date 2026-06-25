using MediatR;

namespace Property.Application.Features.Queries.GetPropertyOwnership;

// Ownership resolution is done in PropertyOwnershipController (Host) via direct
// IPropertyRepository / IBrokerRepository / ICompanyRepository calls because the
// business logic is pure cross-module enrichment with no domain side-effects.
// This query is kept here as a placeholder for future migration to a handler.

public record GetPropertyOwnershipQuery(Guid PropertyId) : IRequest<PropertyOwnershipDto?>;

public enum OwnershipType { DirectOwner, IndependentBroker, CompanyBroker }

public sealed record PropertyOwnershipDto(
    OwnershipType OwnershipType,
    Guid ResponsiblePartyId,
    string? OwnerName,
    bool? IsOwnerVerified,
    string? BrokerName,
    Guid? BrokerId,
    string? CompanyName,
    Guid? CompanyId
);
