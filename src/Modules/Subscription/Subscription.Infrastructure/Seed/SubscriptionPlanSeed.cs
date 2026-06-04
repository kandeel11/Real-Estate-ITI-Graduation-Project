using Microsoft.EntityFrameworkCore;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Seed;

public static class SubscriptionPlanSeed
{
    public static async Task SeedAsync(SubscriptionDbContext context)
    {
        if (await context.Plans.AnyAsync())
            return;

        var plans = GetSeedData();
        await context.Plans.AddRangeAsync(plans);
        await context.SaveChangesAsync();
    }

    public static IEnumerable<SubscriptionPlan> GetSeedData()
    {
        return new[]
        {
            SubscriptionPlan.Create(
                name: "Free Individual",
                tier: SubscriptionTier.FreeIndividual,
                priceMonthly: 0m,
                priceAnnual: 0m,
                maxProperties: 2,
                maxBrokers: 0,
                maxAiRequestsPerMonth: 10,
                maxStorageGb: 1,
                hasAdvancedAnalytics: false,
                hasFeaturedListings: false,
                hasAiPriceEstimation: false,
                hasAiChatbot: false,
                hasAiLeadPrioritization: false,
                hasAiDescriptionGenerator: false),

            SubscriptionPlan.Create(
                name: "Premium Individual",
                tier: SubscriptionTier.PremiumIndividual,
                priceMonthly: 29m,
                priceAnnual: 290m,
                maxProperties: 25,
                maxBrokers: 0,
                maxAiRequestsPerMonth: 500,
                maxStorageGb: 10,
                hasAdvancedAnalytics: true,
                hasFeaturedListings: true,
                hasAiPriceEstimation: true,
                hasAiChatbot: true,
                hasAiLeadPrioritization: false,
                hasAiDescriptionGenerator: true),

            SubscriptionPlan.Create(
                name: "Free Company",
                tier: SubscriptionTier.FreeCompany,
                priceMonthly: 0m,
                priceAnnual: 0m,
                maxProperties: 10,
                maxBrokers: 3,
                maxAiRequestsPerMonth: 50,
                maxStorageGb: 5,
                hasAdvancedAnalytics: false,
                hasFeaturedListings: false,
                hasAiPriceEstimation: false,
                hasAiChatbot: true,
                hasAiLeadPrioritization: false,
                hasAiDescriptionGenerator: false),

            SubscriptionPlan.Create(
                name: "Premium Company",
                tier: SubscriptionTier.PremiumCompany,
                priceMonthly: 99m,
                priceAnnual: 990m,
                maxProperties: 200,
                maxBrokers: 25,
                maxAiRequestsPerMonth: 5000,
                maxStorageGb: 100,
                hasAdvancedAnalytics: true,
                hasFeaturedListings: true,
                hasAiPriceEstimation: true,
                hasAiChatbot: true,
                hasAiLeadPrioritization: true,
                hasAiDescriptionGenerator: true),
        };
    }
}
