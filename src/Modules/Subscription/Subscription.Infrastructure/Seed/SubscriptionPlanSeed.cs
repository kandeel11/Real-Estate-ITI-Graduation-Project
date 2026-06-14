using Microsoft.EntityFrameworkCore;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Seed;

public static class SubscriptionPlanSeed
{
    public static async Task SeedAsync(SubscriptionDbContext context)
    {
        var seed = GetSeedData().ToList();

        if (!await context.Plans.AnyAsync())
        {
            await context.Plans.AddRangeAsync(seed);
            await context.SaveChangesAsync();
            return;
        }

        // Existing database: keep the feature FLAGS in sync with the seed
        // definition (limits/prices are left alone so admin tweaks survive).
        // Needed e.g. when Free Individual gained the chatbot.
        var existing = await context.Plans.ToListAsync();
        var changed = false;
        foreach (var plan in existing)
        {
            var s = seed.FirstOrDefault(x => x.Tier == plan.Tier);
            if (s is null) continue;

            if (plan.HasAdvancedAnalytics != s.HasAdvancedAnalytics ||
                plan.HasFeaturedListings != s.HasFeaturedListings ||
                plan.HasAiPriceEstimation != s.HasAiPriceEstimation ||
                plan.HasAiChatbot != s.HasAiChatbot ||
                plan.HasAiLeadPrioritization != s.HasAiLeadPrioritization ||
                plan.HasAiDescriptionGenerator != s.HasAiDescriptionGenerator)
            {
                plan.UpdateFeatureFlags(
                    s.HasAdvancedAnalytics,
                    s.HasFeaturedListings,
                    s.HasAiPriceEstimation,
                    s.HasAiChatbot,
                    s.HasAiLeadPrioritization,
                    s.HasAiDescriptionGenerator);
                changed = true;
            }
        }

        if (changed)
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
                // Chatbot is included on free so the 10 AI requests/month
                // are actually usable; the description generator stays premium
                hasAiChatbot: true,
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

            SubscriptionPlan.Create(
                name: "Free Tenant",
                tier: SubscriptionTier.FreeTenant,
                priceMonthly: 0m,
                priceAnnual: 0m,
                maxProperties: 0,
                maxBrokers: 0,
                maxAiRequestsPerMonth: 20,
                maxStorageGb: 0,
                hasAdvancedAnalytics: false,
                hasFeaturedListings: false,
                hasAiPriceEstimation: false,
                hasAiChatbot: true,
                hasAiLeadPrioritization: false,
                hasAiDescriptionGenerator: false,
                maxSavedListings: 5,
                maxSavedSearches: 2,
                maxInquiriesPerMonth: 10,
                hasEarlyAccessListings: false,
                hasPriorityBrokerResponse: false),

            SubscriptionPlan.Create(
                name: "Premium Tenant",
                tier: SubscriptionTier.PremiumTenant,
                priceMonthly: 9m,
                priceAnnual: 90m,
                maxProperties: 0,
                maxBrokers: 0,
                maxAiRequestsPerMonth: 500,
                maxStorageGb: 0,
                hasAdvancedAnalytics: false,
                hasFeaturedListings: false,
                hasAiPriceEstimation: true,
                hasAiChatbot: true,
                hasAiLeadPrioritization: false,
                hasAiDescriptionGenerator: false,
                maxSavedListings: 999999,
                maxSavedSearches: 999999,
                maxInquiriesPerMonth: 999999,
                hasEarlyAccessListings: true,
                hasPriorityBrokerResponse: true),
        };
    }
}
