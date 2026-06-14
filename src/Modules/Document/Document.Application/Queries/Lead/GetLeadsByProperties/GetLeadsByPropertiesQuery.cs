using Document.Application.Queries.Lead.GetLeadsByBroker;
using MediatR;

namespace Document.Application.Queries.Lead.GetLeadsByProperties;

/// <summary>
/// All leads tied to the given set of properties — the broker dashboard's
/// pipeline for every listing the broker manages.
/// </summary>
public record GetLeadsByPropertiesQuery(IReadOnlyCollection<Guid> PropertyIds)
    : IRequest<List<LeadDto>>;
