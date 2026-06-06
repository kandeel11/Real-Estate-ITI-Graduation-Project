using MediatR;

namespace Document.Application.Commands.Lead.UpdateLeadStatus;

public record UpdateLeadStatusCommand(
    Guid LeadId,
    string NewStatus,
    decimal? DealValue = null,
    string? Notes = null) : IRequest;