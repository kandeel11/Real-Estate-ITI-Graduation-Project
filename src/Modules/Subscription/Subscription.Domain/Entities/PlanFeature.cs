using Domain.Entities;

namespace Subscription.Domain.Entities;

public class PlanFeature : BaseEntity
{
    public Guid PlanId { get; private set; }
    public string FeatureKey { get; private set; } = string.Empty;
    public string FeatureLabel { get; private set; } = string.Empty;
    public string FeatureValue { get; private set; } = string.Empty;

    public SubscriptionPlan Plan { get; private set; } = null!;

    private PlanFeature() { }

    public static PlanFeature Create(Guid planId, string featureKey, string featureLabel, string featureValue)
    {
        return new PlanFeature
        {
            PlanId = planId,
            FeatureKey = featureKey,
            FeatureLabel = featureLabel,
            FeatureValue = featureValue
        };
    }
}
