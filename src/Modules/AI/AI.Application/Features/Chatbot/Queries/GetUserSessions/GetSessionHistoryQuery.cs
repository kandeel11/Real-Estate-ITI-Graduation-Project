using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AI.Application.Features.Chatbot.Queries.GetSessionHistory;

// 1. الـ Record الخاص بالـ Query (بياخد Guid)
public record GetSessionHistoryQuery(Guid SessionId) : IRequest<IReadOnlyList<ChatbotMessageDto>>;

// 2. الـ DTO الخاص بالرسالة (عشان مانستخدمش SessionMessage بتاعة الـ RAG)
public record ChatbotMessageDto(string Role, string Content);

// 3. الـ Handler
public class GetSessionHistoryQueryHandler : IRequestHandler<GetSessionHistoryQuery, IReadOnlyList<ChatbotMessageDto>>
{
    private readonly IAiDbContext _context;

    public GetSessionHistoryQueryHandler(IAiDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ChatbotMessageDto>> Handle(
        GetSessionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // بنجيب الرسايل مباشرة من جداول الشات العادي
        var history = await _context.ChatbotMessages
            .AsNoTracking()
            .Where(m => m.SessionId == query.SessionId && m.Role != "system")
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatbotMessageDto(m.Role, m.Content)) // بنعمل Map للـ DTO الجديد
            .ToListAsync(cancellationToken);

        return history;
    }
}