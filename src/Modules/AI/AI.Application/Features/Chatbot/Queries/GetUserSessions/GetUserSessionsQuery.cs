using AI.Application.DTOs;
using AI.Application.Interfaces.Common;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AI.Application.Features.Chatbot.Queries.GetUserSessions;

public record GetUserSessionsQuery : IRequest<List<ChatbotSessionSummaryDto>>;

public class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, List<ChatbotSessionSummaryDto>>
{
    private readonly IAiDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserSessionsQueryHandler(IAiDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ChatbotSessionSummaryDto>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? Guid.Empty;

        // بنجيب السيشنز الخاصة باليوزر ده بس، ونرتبها من الأحدث للأقدم
        return await _context.ChatbotSessions
            .AsNoTracking() // مهم جداً للأداء في الـ Queries
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ChatbotSessionSummaryDto
            {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}