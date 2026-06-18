using Document.Application.Queries.Lead.GetLeadsByBroker;
using MediatR;

namespace Document.Application.Queries.Lead.GetAllLeads;

public record GetAllLeadsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null) : IRequest<List<LeadDto>>;
