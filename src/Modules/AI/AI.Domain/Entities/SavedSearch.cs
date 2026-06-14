// AI.Domain/Entities/SavedSearch.cs

namespace AI.Domain.Entities;

/// <summary>
/// User saves a search query to get notified when new properties match.
/// Stored in ai.saved_searches.
/// Premium plan feature only.
/// </summary>
public sealed class SavedSearch
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = default!;
    public string RawQuery { get; private set; } = default!;
    public string ParsedFilters { get; private set; } = default!; // JSON
    public bool NotifyOnMatch { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SavedSearch() { }

    public static SavedSearch Create(
        Guid userId,
        string name,
        string rawQuery,
        string parsedFiltersJson,
        bool notifyOnMatch) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            RawQuery = rawQuery,
            ParsedFilters = parsedFiltersJson,
            NotifyOnMatch = notifyOnMatch,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotification(bool notifyOnMatch)
    {
        NotifyOnMatch = notifyOnMatch;
        UpdatedAt = DateTime.UtcNow;
    }
}