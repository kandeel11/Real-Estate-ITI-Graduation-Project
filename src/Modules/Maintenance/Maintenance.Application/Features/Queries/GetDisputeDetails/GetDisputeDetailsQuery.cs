using MediatR;
using Maintenance.Application.DTOs.Responses;

namespace Maintenance.Application.Features.Queries.GetDisputeDetails;

public record GetDisputeDetailsQuery(Guid TicketId) : IRequest<DisputeDetailsDto>;
