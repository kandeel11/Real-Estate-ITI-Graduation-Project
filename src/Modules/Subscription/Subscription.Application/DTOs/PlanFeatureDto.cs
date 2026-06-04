namespace Subscription.Application.DTOs;

public record PlanFeatureDto
{
    public Guid Id { get; init; }
    public Guid PlanId { get; init; }
    public string FeatureKey { get; init; } = string.Empty;
    public string FeatureLabel { get; init; } = string.Empty;
    public string FeatureValue { get; init; } = string.Empty;
}
