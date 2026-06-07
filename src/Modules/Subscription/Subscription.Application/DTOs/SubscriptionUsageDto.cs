namespace Subscription.Application.DTOs;

public record SubscriptionUsageDto
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public int PropertiesUsed { get; init; }
    public int BrokersUsed { get; init; }
    public int AiRequestsUsed { get; init; }
    public decimal StorageUsedGb { get; init; }
}
