using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPropertiesByBroker;

/// <summary>
/// Properties whose ListingBrokerId is the given broker's UserId.
/// Brokers reference the identity user id everywhere (see PropertyEnrichmentService).
/// </summary>
public record GetPropertiesByBrokerQuery(Guid BrokerUserId) : IRequest<List<PropertySummaryDto>>;
