using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;
using MediatR;

namespace Maintenance.Application.Features.Queries.GetAvailableVendors;

public record GetAvailableVendorsForTicketQuery(Guid TicketId) : IRequest<IReadOnlyList<AvailableVendorDto>>;

public class GetAvailableVendorsForTicketHandler(
    IMaintenanceTicketRepository ticketRepo,
    ITicketAiEvaluationRepository aiRepo,
    IAutoVendorAssignmentService autoAssign)
    : IRequestHandler<GetAvailableVendorsForTicketQuery, IReadOnlyList<AvailableVendorDto>>
{
    public async Task<IReadOnlyList<AvailableVendorDto>> Handle(
        GetAvailableVendorsForTicketQuery q, CancellationToken ct)
    {
        var ticket = await ticketRepo.GetByIdWithDetailsAsync(q.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {q.TicketId} not found.");

        var evaluation = await aiRepo.GetByTicketIdAsync(q.TicketId, ct)
            ?? throw new InvalidOperationException(
                "AI evaluation not yet available for this ticket.");

        var city = ExtractCity(ticket.PropertyAddress);

        return await autoAssign.GetAvailableVendorsAsync(evaluation.Category, city, ct);
    }

    // Extracts city from "12 Nile St, Dokki, Cairo, Egypt" → "Cairo"
    private static string ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        var parts = address.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }
}
