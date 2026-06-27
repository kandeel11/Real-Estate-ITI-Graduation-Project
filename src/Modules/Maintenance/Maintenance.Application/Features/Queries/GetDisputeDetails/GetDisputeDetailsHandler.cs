using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Queries.GetDisputeDetails;

public class GetDisputeDetailsHandler(
    IMaintenanceTicketRepository repo,
    ILeaseDetailsAdapter leaseAdapter)
    : IRequestHandler<GetDisputeDetailsQuery, DisputeDetailsDto>
{
    public async Task<DisputeDetailsDto> Handle(GetDisputeDetailsQuery query, CancellationToken ct)
    {
        var ticket = await repo.GetByIdWithDetailsAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {query.TicketId} not found.");

        AiEvaluationDto? aiDto = ticket.AiEvaluation is { } ai
            ? new AiEvaluationDto(ai.Id, ai.ResponsibleParty, ai.Category, ai.ConfidenceScore, ai.Reasoning, ai.EvaluatedAt)
            : null;

        var images = ticket.Attachments
            .Where(a => a.DeletedAt == null && a.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .Select(a => new AttachmentDto(a.Id, a.FileUrl, a.FileType, a.FileSizeKb, a.CreatedAt))
            .ToList();

        LeaseContractSummaryDto? lease = null;
        if (ticket.LeaseId.HasValue)
            lease = await leaseAdapter.GetLeaseContractAsync(ticket.LeaseId.Value, ct);

        return new DisputeDetailsDto(
            TicketId: ticket.Id,
            TicketNumber: ticket.TicketNumber,
            Title: ticket.Title,
            Description: ticket.Description,
            Priority: ticket.Priority.ToString(),
            Status: ticket.Status.ToString(),
            PropertyAddress: ticket.PropertyAddress,
            PropertyId: ticket.PropertyId,
            TenantId: ticket.TenantId,
            OwnerId: ticket.OwnerId,
            CreatedAt: ticket.CreatedAt,
            AiEvaluation: aiDto,
            Images: images,
            DisputedById: ticket.DisputedById,
            DisputedByRole: ticket.DisputedByRole,
            DisputeReason: ticket.DisputeReason,
            DisputeDate: ticket.DisputeDate,
            AdminNotes: ticket.AdminNotes,
            LeaseContract: lease
        );
    }
}
