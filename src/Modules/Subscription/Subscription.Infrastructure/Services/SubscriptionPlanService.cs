using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Subscription.Infrastructure.Persistence;

namespace Subscription.Infrastructure.Services;

/// <summary>
/// Implementation حقيقي لـ ISubscriptionPlanService (المُعرَّف في Payment.Application)
/// بيقرأ مباشرة من SubscriptionDbContext — مفيش أي بيانات Mock أو Hardcoded
///
/// ده الـ Adapter اللي يحل مشكلة PlanNotFoundException القديمة:
/// كانت MockSubscriptionPlanService بترجع IDs ثابتة (aaaaaaaa-0000...)
/// مش موجودة في الداتابيز الحقيقية، فكانت كل عملية CreateSubscriptionPayment
/// بترمي PlanNotFoundException.
///
/// مكان التسجيل: Subscription.Infrastructure.SubscriptionModuleExtensions
/// (AddSubscriptionModule لازم يتم استدعاؤه بعد AddPaymentModule في Program.cs)
/// </summary>
public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionPlanService(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlanDto?> GetPlanByIdAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan is null)
            return null;

        // Quarterly مش مخزّن في SubscriptionPlan (بس فيه Monthly و Annual) —
        // بنحسبه كـ 3 × السعر الشهري (يقدر يتغير لو الـ business عايز خصم مختلف)
        var quarterlyPrice = plan.PriceMonthly * 3;

        return new SubscriptionPlanDto(
            PlanId: plan.Id,
            Name: plan.Name,
            MonthlyPrice: plan.PriceMonthly,
            QuarterlyPrice: quarterlyPrice,
            AnnualPrice: plan.PriceAnnual,
            IsActive: plan.IsActive);
    }

    public async Task<SubscriptionDto?> GetSubscriptionByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

        if (subscription is null)
            return null;

        return new SubscriptionDto(
            SubscriptionId: subscription.Id,
            UserId: subscription.UserId,
            PlanId: subscription.PlanId,
            Status: subscription.Status.ToString(),
            CurrentPeriodEnd: subscription.EndDate);
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId &&
                        (s.Status == Domain.Enums.SubscriptionStatus.Active ||
                         s.Status == Domain.Enums.SubscriptionStatus.Trial))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
            return null;

        return new SubscriptionDto(
            SubscriptionId: subscription.Id,
            UserId: subscription.UserId,
            PlanId: subscription.PlanId,
            Status: subscription.Status.ToString(),
            CurrentPeriodEnd: subscription.EndDate);
    }
}
