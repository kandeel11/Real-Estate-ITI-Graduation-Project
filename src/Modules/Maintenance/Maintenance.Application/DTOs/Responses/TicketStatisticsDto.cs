namespace Maintenance.Application.DTOs.Responses;

public record TicketStatisticsDto(
    int Total, int Draft, int Submitted, int UnderAiReview,
    int WaitingForDecision, int AssignedToVendor, int InProgress,
    int AwaitingPayment, int Completed, int Cancelled, int Rejected);