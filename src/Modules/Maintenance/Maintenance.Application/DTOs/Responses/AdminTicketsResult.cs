namespace Maintenance.Application.DTOs.Responses;

public record AdminTicketsResult(
    List<AdminTicketDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    TicketStatisticsDto Statistics
);
