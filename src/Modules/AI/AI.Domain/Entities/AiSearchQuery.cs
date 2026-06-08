// AI.Domain/Entities/AiSearchQuery.cs

namespace AI.Domain.Entities;

/// <summary>
/// Every smart search query logged here for analytics.
/// Stored in ai.ai_search_queries.
/// </summary>
public sealed class AiSearchQuery
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public string RawQuery { get; private set; } = default!;
    public string Language { get; private set; } = default!; // "ar" | "en" | "mixed"
    public string ParsedFilters { get; private set; } = default!; // JSON
    public int ResultCount { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal ParsingConfidence { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AiSearchQuery() { }

    public static AiSearchQuery Create(
        Guid? userId,
        string rawQuery,
        string language,
        string parsedFiltersJson,
        int resultCount,
        int tokensUsed,
        decimal confidence) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawQuery = rawQuery,
            Language = language,
            ParsedFilters = parsedFiltersJson,
            ResultCount = resultCount,
            TokensUsed = tokensUsed,
            ParsingConfidence = confidence,
            CreatedAt = DateTime.UtcNow
        };
}