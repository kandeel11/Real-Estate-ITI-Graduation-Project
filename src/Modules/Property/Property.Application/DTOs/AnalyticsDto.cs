namespace Property.Application.DTOs;

public class AnalyticsDto
{
    public Guid PropertyId { get; init; }
    public int TotalViews { get; init; }
    public List<DailyViewDto> ViewsByDay { get; init; } = [];
    public List<SourceBreakdownDto> SourceBreakdown { get; init; } = [];
}

public class DailyViewDto
{
    public DateOnly Date { get; init; }
    public int Views { get; init; }
}

public class SourceBreakdownDto
{
    public string Source { get; init; } = string.Empty;
    public int Views { get; init; }
}
