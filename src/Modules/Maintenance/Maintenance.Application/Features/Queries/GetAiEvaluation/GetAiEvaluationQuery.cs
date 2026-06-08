using MediatR;
using Maintenance.Application.DTOs.Responses;
using Maintenance.Application.Interfaces;

namespace Maintenance.Application.Features.Queries.GetAiEvaluation;

public record GetAiEvaluationQuery(Guid TicketId) : IRequest<AiEvaluationDto?>;

public class GetAiEvaluationHandler(ITicketAiEvaluationRepository repo)
    : IRequestHandler<GetAiEvaluationQuery, AiEvaluationDto?>
{
    public async Task<AiEvaluationDto?> Handle(GetAiEvaluationQuery q, CancellationToken ct)
    {
        var e = await repo.GetByTicketIdAsync(q.TicketId, ct);
        if (e is null) return null;
        return new AiEvaluationDto(e.Id, e.ResponsibleParty, e.Category,
            e.ConfidenceScore, e.Reasoning, e.EvaluatedAt);
    }
}