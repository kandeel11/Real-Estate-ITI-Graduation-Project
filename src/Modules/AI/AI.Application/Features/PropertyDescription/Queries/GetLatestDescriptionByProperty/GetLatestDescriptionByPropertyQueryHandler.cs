using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AI.Application.Features.PropertyDescription.Queries.GetLatestDescriptionByProperty;

public class GetLatestDescriptionByPropertyQueryHandler
    : IRequestHandler<GetLatestDescriptionByPropertyQuery, DescriptionDraftDto?>
{
    private readonly IAiDbContext _context;

    public GetLatestDescriptionByPropertyQueryHandler(IAiDbContext context)
    {
        _context = context;
    }

    public async Task<DescriptionDraftDto?> Handle(GetLatestDescriptionByPropertyQuery request, CancellationToken cancellationToken)
    {
        var record = await _context.AiDescriptionGenerations
            .Where(g => g.PropertyId == request.PropertyId)
            .OrderByDescending(g => g.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return null;

        return new DescriptionDraftDto(
            GenerationId: record.Id,
            PropertyId: record.PropertyId,
            GeneratedTextEn: record.GeneratedTextEn,
            GeneratedTextAr: record.GeneratedTextAr,
            IsAccepted: record.IsAccepted,
            CreatedAt: record.CreatedAt);
    }
}
