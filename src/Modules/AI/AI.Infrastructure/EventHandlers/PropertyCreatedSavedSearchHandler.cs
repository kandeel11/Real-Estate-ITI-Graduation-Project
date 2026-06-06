//// AI.Infrastructure/EventHandlers/PropertyCreatedSavedSearchHandler.cs

//using AI.Application.DTOs;
//using AI.Application.Events;
//using AI.Application.Interfaces;
//using MediatR;
//using Microsoft.Extensions.Logging;
//using Properties.Application.Events;   // ← Ahmed's event
//using Property.Domain.Events;
//using System.Text.Json;

//namespace AI.Infrastructure.EventHandlers;

///// <summary>
///// When a new property is created, check ALL active saved searches
///// with notify_on_match = true.
///// If property matches saved search filters → notify user.
///// </summary>
//public sealed class PropertyCreatedSavedSearchHandler
//    : INotificationHandler<PropertyCreatedEvent>
//{
//    private readonly ISavedSearchRepository _savedSearchRepo;
//    private readonly IPropertyFilterService _propertyFilter;
//    private readonly IPublisher _publisher;
//    private readonly ILogger<PropertyCreatedSavedSearchHandler> _logger;

//    public PropertyCreatedSavedSearchHandler(
//        ISavedSearchRepository savedSearchRepo,
//        IPropertyFilterService propertyFilter,
//        IPublisher publisher,
//        ILogger<PropertyCreatedSavedSearchHandler> logger)
//    {
//        _savedSearchRepo = savedSearchRepo;
//        _propertyFilter = propertyFilter;
//        _publisher = publisher;
//        _logger = logger;
//    }

//    public async Task Handle(
//        PropertyCreatedEvent notification,
//        CancellationToken cancellationToken)
//    {
//        _logger.LogInformation(
//            "Checking new property {PropertyId} against saved searches",
//            notification.PropertyId);

//        // Load all active saved searches with notifications enabled
//        var savedSearches = await _savedSearchRepo
//            .GetActiveWithNotificationsAsync(cancellationToken);

//        if (savedSearches.Count == 0) return;

//        _logger.LogInformation(
//            "Checking {Count} active saved searches", savedSearches.Count);

//        foreach (var savedSearch in savedSearches)
//        {
//            try
//            {
//                var filters = JsonSerializer
//                    .Deserialize<ParsedSearchFilters>(savedSearch.ParsedFilters)!;

//                // Check if this property matches the saved search filters
//                var matches = await _propertyFilter.PropertyMatchesFiltersAsync(
//                    notification.PropertyId, filters, cancellationToken);

//                if (!matches) continue;

//                _logger.LogInformation(
//                    "Property {PropertyId} matches saved search {SearchId} " +
//                    "for user {UserId}",
//                    notification.PropertyId, savedSearch.Id, savedSearch.UserId);

//                // Notify user → Notifications module handles sending
//                await _publisher.Publish(
//                    new PropertyMatchesSavedSearchEvent(
//                        SavedSearchId: savedSearch.Id,
//                        UserId: savedSearch.UserId,
//                        SavedSearchName: savedSearch.Name,
//                        PropertyId: notification.PropertyId,
//                        PropertyTitle: notification.Title,
//                        City: notification.City,
//                        District: notification.District,
//                        Price: notification.Price),
//                    cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex,
//                    "Error checking saved search {SearchId}", savedSearch.Id);
//                // Continue with next saved search — don't fail entire batch
//            }
//        }
//    }
//}