namespace Payment.Application.Interfaces;

/// <summary>
/// Contract للتواصل مع Subscription Module — بدون أي dependency مباشر
/// على Infrastructure بتاعه (Dependency Inversion)
///
/// Payment.Application هو اللي بيعرّف الـ interface ده،
/// لكن الـ implementation موجود في Subscription.Infrastructure
/// (Adapter Pattern) — وده اللي يمنع الـ tight coupling
/// </summary>
public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanDto?> GetPlanByIdAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    Task<SubscriptionDto?> GetActiveSubscriptionForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// بيانات الخطة اللي Payment module محتاجها
/// </summary>
public sealed record SubscriptionPlanDto(
    Guid PlanId,
    string Name,
    decimal MonthlyPrice,
    decimal QuarterlyPrice,
    decimal AnnualPrice,
    bool IsActive);

/// <summary>
/// بيانات الاشتراك اللي Payment module محتاجها
/// </summary>
public sealed record SubscriptionDto(
    Guid SubscriptionId,
    Guid UserId,
    Guid PlanId,
    string Status,           // Active, Expired, Cancelled, PendingPayment
    DateTime? CurrentPeriodEnd
);
