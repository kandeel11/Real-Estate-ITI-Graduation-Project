// AI.Application/Events/PropertyMatchesSavedSearchEvent.cs

using MediatR;

namespace AI.Application.Events;

/// <summary>
/// Published when a new property matches a user's saved search.
/// Notifications module subscribes → sends push/email to user.
/// </summary>
public sealed record PropertyMatchesSavedSearchEvent(
    Guid SavedSearchId,
    Guid UserId,
    string SavedSearchName,
    Guid PropertyId,
    string PropertyTitle,
    string City,
    string District,
    decimal Price
) : INotification;