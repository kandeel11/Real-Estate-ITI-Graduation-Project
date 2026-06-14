using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Subscription.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Central domain-event dispatcher for the Subscription module.
///
/// After EF Core commits a SaveChanges, this scans every tracked entity for queued
/// domain events and publishes them through MediatR. Subscription entities raise
/// SubscriptionCreated/Cancelled/Upgraded/PaymentProcessed events; before this
/// interceptor those were attached to entities but never published.
///
/// Events are collected and cleared BEFORE publishing so that if a handler triggers
/// another SaveChanges on the same context, it won't re-dispatch the same events.
/// </summary>
public sealed class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DispatchDomainEventsInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchEventsAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
            DispatchEventsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchEventsAsync(DbContext context, CancellationToken ct)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0)
            return;

        // Snapshot then clear first — prevents re-dispatch if a handler saves again.
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, ct);
    }
}
