using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RealEstate.Shared.Kernel.Analytics;
using System.ComponentModel;
using System.Text.Json;

namespace AI.Application.Features.AdminAnalytics.Plugins;

/// <summary>
/// Semantic Kernel native plugin that gives the Admin Analytics chatbot
/// the ability to query live data across all system modules.
///
/// Each method maps to one answerable question domain. The Kernel selects
/// and invokes these automatically based on the admin's natural language prompt.
///
/// Boundary rule: this class only depends on Shared analytics interfaces —
/// never on a module DbContext or repository directly.
/// </summary>
public sealed class AdminAnalyticsPlugin
{
    private readonly IPropertyAnalyticsService _properties;
    private readonly IPaymentAnalyticsService _payments;
    private readonly ISubscriptionAnalyticsService _subscriptions;
    private readonly IUserAnalyticsService _users;
    private readonly ILogger<AdminAnalyticsPlugin> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public AdminAnalyticsPlugin(
        IPropertyAnalyticsService properties,
        IPaymentAnalyticsService payments,
        ISubscriptionAnalyticsService subscriptions,
        IUserAnalyticsService users,
        ILogger<AdminAnalyticsPlugin> logger)
    {
        _properties = properties;
        _payments = payments;
        _subscriptions = subscriptions;
        _users = users;
        _logger = logger;
    }

    // ── Property ─────────────────────────────────────────────────────────────

    [KernelFunction("GetPropertyOverview")]
    [Description("""
        Returns a full overview of all properties in the system: total count,
        active listings, rented properties, properties pending verification,
        rejected properties, and the number of currently active leases.
        Use this for any general question about property counts or status distribution.
        """)]
    public async Task<string> GetPropertyOverviewAsync()
    {
        try
        {
            var summary = await _properties.GetSummaryAsync();
            return JsonSerializer.Serialize(summary, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve property overview");
            return "Unable to retrieve property data at this time.";
        }
    }

    [KernelFunction("GetNewPropertyListings")]
    [Description("""
        Returns the number of new property listings created within a specific date range.
        Use for questions like 'How many properties were added this week/month/today?'
        Dates must be in yyyy-MM-dd format (e.g. '2025-06-01').
        """)]
    public async Task<string> GetNewPropertyListingsAsync(
        [Description("Start of the date range in yyyy-MM-dd format")] string from,
        [Description("End of the date range in yyyy-MM-dd format")] string to)
    {
        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return $"Invalid date format. Received from='{from}', to='{to}'. Use yyyy-MM-dd.";

        try
        {
            var count = await _properties.GetNewListingsCountAsync(fromDate, toDate);
            return JsonSerializer.Serialize(new { NewListings = count, From = from, To = to }, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve new listings count for range {From}-{To}", from, to);
            return "Unable to retrieve new listings data at this time.";
        }
    }

    [KernelFunction("GetTopCitiesByListings")]
    [Description("""
        Returns the cities with the most property listings, ranked in descending order.
        Use for questions like 'Which cities are most active?' or 'Where are most properties listed?'
        """)]
    public async Task<string> GetTopCitiesByListingsAsync(
        [Description("How many top cities to return (default 5, max 10)")] int topN = 5)
    {
        topN = Math.Clamp(topN, 1, 10);

        try
        {
            var cities = await _properties.GetTopCitiesAsync(topN);
            return JsonSerializer.Serialize(cities, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top cities");
            return "Unable to retrieve city data at this time.";
        }
    }

    [KernelFunction("GetPropertyBreakdownByType")]
    [Description("""
        Returns the count of properties grouped by type (Apartment, Villa, Studio, etc.).
        Use for questions like 'How many apartments vs villas are listed?'
        """)]
    public async Task<string> GetPropertyBreakdownByTypeAsync()
    {
        try
        {
            var breakdown = await _properties.GetCountsByTypeAsync();
            return JsonSerializer.Serialize(breakdown, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve property type breakdown");
            return "Unable to retrieve property type data at this time.";
        }
    }

    // ── Payment ───────────────────────────────────────────────────────────────

    [KernelFunction("GetPaymentRevenueSummary")]
    [Description("""
        Returns an overall rent-payment revenue summary: total amount collected,
        total pending, total late fees, number of overdue payments, and total paid count.
        Use for general revenue questions like 'What is the total revenue?' or
        'How many payments are overdue?'
        """)]
    public async Task<string> GetPaymentRevenueSummaryAsync()
    {
        try
        {
            var summary = await _payments.GetSummaryAsync();
            return JsonSerializer.Serialize(summary, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve payment revenue summary");
            return "Unable to retrieve payment data at this time.";
        }
    }

    [KernelFunction("GetPaymentRevenueInRange")]
    [Description("""
        Returns the total rent revenue collected within a specific date range.
        Use for questions like 'What was the revenue in June?' or
        'How much was collected this quarter?'
        Dates must be in yyyy-MM-dd format.
        """)]
    public async Task<string> GetPaymentRevenueInRangeAsync(
        [Description("Start of the date range in yyyy-MM-dd format")] string from,
        [Description("End of the date range in yyyy-MM-dd format")] string to)
    {
        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return $"Invalid date format. Received from='{from}', to='{to}'. Use yyyy-MM-dd.";

        try
        {
            var revenue = await _payments.GetRevenueInRangeAsync(fromDate, toDate);
            return JsonSerializer.Serialize(new { RevenueCollected = revenue, From = from, To = to }, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve revenue for range {From}-{To}", from, to);
            return "Unable to retrieve revenue data at this time.";
        }
    }

    // ── Subscription ──────────────────────────────────────────────────────────

    [KernelFunction("GetSubscriptionOverview")]
    [Description("""
        Returns a subscription summary: total active, trial, expired/cancelled subscriptions,
        and total revenue collected from subscriptions.
        Use for questions like 'How many active subscriptions are there?' or
        'What is the total subscription revenue?'
        """)]
    public async Task<string> GetSubscriptionOverviewAsync()
    {
        try
        {
            var summary = await _subscriptions.GetSummaryAsync();
            return JsonSerializer.Serialize(summary, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription overview");
            return "Unable to retrieve subscription data at this time.";
        }
    }

    [KernelFunction("GetSubscriptionsByPlan")]
    [Description("""
        Returns the number of active subscriptions grouped by plan name and tier
        (e.g. Free, Basic, Professional, Enterprise).
        Use for questions like 'Which plan is most popular?' or
        'How many users are on the Professional plan?'
        """)]
    public async Task<string> GetSubscriptionsByPlanAsync()
    {
        try
        {
            var plans = await _subscriptions.GetCountsByPlanAsync();
            return JsonSerializer.Serialize(plans, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription plan breakdown");
            return "Unable to retrieve plan data at this time.";
        }
    }

    [KernelFunction("GetSubscriptionActivity")]
    [Description("""
        Returns the count of new subscriptions started and subscriptions churned
        (cancelled or expired) within a specific date range.
        Use for questions like 'How many new subscriptions did we get this month?' or
        'What is the churn rate this quarter?'
        Dates must be in yyyy-MM-dd format.
        """)]
    public async Task<string> GetSubscriptionActivityAsync(
        [Description("Start of the date range in yyyy-MM-dd format")] string from,
        [Description("End of the date range in yyyy-MM-dd format")] string to)
    {
        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return $"Invalid date format. Received from='{from}', to='{to}'. Use yyyy-MM-dd.";

        try
        {
            var newCount = await _subscriptions.GetNewSubscriptionsCountAsync(fromDate, toDate);
            var churnedCount = await _subscriptions.GetChurnedSubscriptionsCountAsync(fromDate, toDate);

            return JsonSerializer.Serialize(
                new { NewSubscriptions = newCount, ChurnedSubscriptions = churnedCount, From = from, To = to },
                JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription activity for range {From}-{To}", from, to);
            return "Unable to retrieve subscription activity data at this time.";
        }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [KernelFunction("GetUserOverview")]
    [Description("""
        Returns the total count of registered users, companies, and brokers in the system.
        Use for questions like 'How many users do we have?' or 'How many companies are registered?'
        """)]
    public async Task<string> GetUserOverviewAsync()
    {
        try
        {
            var summary = await _users.GetSummaryAsync();
            return JsonSerializer.Serialize(summary, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user overview");
            return "Unable to retrieve user data at this time.";
        }
    }

    [KernelFunction("GetNewUsersInRange")]
    [Description("""
        Returns the number of new users who registered within a specific date range.
        Use for questions like 'How many users signed up this week?' or
        'What was user growth in the last month?'
        Dates must be in yyyy-MM-dd format.
        """)]
    public async Task<string> GetNewUsersInRangeAsync(
        [Description("Start of the date range in yyyy-MM-dd format")] string from,
        [Description("End of the date range in yyyy-MM-dd format")] string to)
    {
        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
            return $"Invalid date format. Received from='{from}', to='{to}'. Use yyyy-MM-dd.";

        try
        {
            var count = await _users.GetNewUsersCountAsync(fromDate, toDate);
            return JsonSerializer.Serialize(new { NewUsers = count, From = from, To = to }, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve new user count for range {From}-{To}", from, to);
            return "Unable to retrieve user registration data at this time.";
        }
    }
}
