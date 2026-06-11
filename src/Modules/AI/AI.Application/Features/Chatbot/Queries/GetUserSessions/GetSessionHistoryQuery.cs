using AI.Application.Features.Chat.Queries;
using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AI.Application.Features.Chatbot.Queries.GetSessionHistory;

public class GetSessionHistoryQueryHandler : IRequestHandler<GetSessionHistoryQuery, IReadOnlyList<SessionMessage>>
{
    private readonly IAiDbContext _context; // عملنا Inject للـ DbContext هنا

    public GetSessionHistoryQueryHandler(IAiDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SessionMessage>> Handle(
        GetSessionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // 1. بنجيب الرسايل مباشرة من الداتابيز لسيشن معينة
        var history = await _context.ChatbotMessages
            .AsNoTracking() // AsNoTracking لتحسين الأداء لأنها عملية قراءة فقط
            .Where(m => m.SessionId == query.SessionId && m.Role != "system") // فلترة الـ system prompt زي ما كنت عامل
            .OrderBy(m => m.CreatedAt) // ترتيب الرسايل من الأقدم للأحدث (الـ CreatedAt موروثة من BaseEntity)
            .Select(m => new SessionMessage(m.Role, m.Content)) // عمل الـ Mapping للـ DTO/Record بتاعك
            .ToListAsync(cancellationToken);

        return history;
    }
}