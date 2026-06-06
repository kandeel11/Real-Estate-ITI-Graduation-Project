//// AI.Infrastructure/EventHandlers/PropertyDuplicateCheckHandler.cs

//using AI.Application.DTOs;
//using AI.Application.Events;
//using AI.Application.Interfaces;
//using MediatR;
//using Microsoft.Extensions.Logging;
//using Properties.Application.Events;   // ← Yasser's event

//namespace AI.Infrastructure.EventHandlers;

///// <summary>
///// Mohamed subscribes to PropertyCreatedEvent.
///// Runs duplicate check → publishes result.
///// If not duplicate → also triggers description generation.
///// </summary>
//public sealed class PropertyDuplicateCheckHandler
//    : INotificationHandler<PropertyCreatedEvent>
//{
//    private readonly IPropertyDuplicateCheckService _duplicateCheck;
//    private readonly IPropertyDescriptionService _description;
//    private readonly IPublisher _publisher;
//    private readonly ILogger<PropertyDuplicateCheckHandler> _logger;

//    public PropertyDuplicateCheckHandler(
//        IPropertyDuplicateCheckService duplicateCheck,
//        IPropertyDescriptionService description,
//        IPublisher publisher,
//        ILogger<PropertyDuplicateCheckHandler> logger)
//    {
//        _duplicateCheck = duplicateCheck;
//        _description = description;
//        _publisher = publisher;
//        _logger = logger;
//    }

//    public async Task Handle(
//        PropertyCreatedEvent notification,
//        CancellationToken cancellationToken)
//    {
//        _logger.LogInformation(
//            "AI received PropertyCreatedEvent | PropertyId={Id} | {Title}",
//            notification.PropertyId, notification.Title);

//        try
//        {
//            // ── Run duplicate check ───────────────────────────────────────────
//            var result = await _duplicateCheck.CheckAsync(
//                new PropertyDuplicateCheckRequest(
//                    PropertyId: notification.PropertyId,
//                    OwnerId: notification.OwnerId,
//                    Title: notification.Title,
//                    Description: notification.AdditionalNotes ?? string.Empty,
//                    City: notification.City,
//                    District: notification.District,
//                    Street: notification.Street,
//                    BuildingName: notification.BuildingName,
//                    UnitNumber: notification.UnitNumber,
//                    Floor: notification.Floor,
//                    PropertyType: notification.PropertyType,
//                    ListingType: notification.ListingType,
//                    AreaInMeters: notification.AreaInMeters,
//                    Bedrooms: notification.Bedrooms,
//                    Bathrooms: notification.Bathrooms,
//                    Price: notification.Price),
//                cancellationToken);

//            // ── Publish duplicate check result → Properties module handles ────
//            await _publisher.Publish(
//                new PropertyDuplicateCheckCompletedEvent(
//                    PropertyId: notification.PropertyId,
//                    IsDuplicate: result.IsDuplicate,
//                    HighestSimilarityScore: result.HighestSimilarityScore,
//                    SimilarProperties: result.SimilarProperties,
//                    Reasoning: result.Reasoning,
//                    AdminNote: result.AdminNote),
//                cancellationToken);

//            // ── If NOT duplicate → generate bilingual description ─────────────
//            if (!result.IsDuplicate)
//            {
//                _logger.LogInformation(
//                    "Property {Id} passed duplicate check — generating description",
//                    notification.PropertyId);

//                var description = await _description.GenerateAsync(
//                    new PropertyDescriptionRequest(
//                        PropertyId: notification.PropertyId,
//                        Title: notification.Title,
//                        PropertyType: notification.PropertyType,
//                        City: notification.City,
//                        District: notification.District,
//                        AreaInMeters: notification.AreaInMeters,
//                        Bedrooms: notification.Bedrooms,
//                        Bathrooms: notification.Bathrooms,
//                        Floor: notification.Floor,
//                        Price: notification.Price,
//                        ListingType: notification.ListingType,
//                        AdditionalNotes: notification.AdditionalNotes),
//                    cancellationToken);

//                await _publisher.Publish(
//                    new PropertyDescriptionGeneratedEvent(
//                        notification.PropertyId,
//                        description,
//                        "bilingual"),
//                    cancellationToken);
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex,
//                "Duplicate check pipeline failed for PropertyId={Id}",
//                notification.PropertyId);

//            await _publisher.Publish(
//                new PropertyDuplicateCheckFailedEvent(
//                    notification.PropertyId,
//                    ex.Message),
//                cancellationToken);
//        }
//    }
//}