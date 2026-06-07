using MediatR;
using Microsoft.Extensions.Logging;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace AI.Application.Features.DuplicateDetection;

public class PropertySubmittedForReviewHandler
    : INotificationHandler<PropertySubmittedForReviewEvent>
{
    private readonly IPublisher _publisher;
    private readonly ILogger<PropertySubmittedForReviewHandler> _logger;

    public PropertySubmittedForReviewHandler(
        IPublisher publisher,
        ILogger<PropertySubmittedForReviewHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(PropertySubmittedForReviewEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Running duplicate detection for property {PropertyId}", notification.PropertyId);

            // TODO (Mohamed): Call your duplicate detection logic here.
            // You have all property data available on `notification`:
            //   notification.Title, notification.Description
            //   notification.City, notification.District, notification.Address
            //   notification.ApartmentNumber, notification.FloorNumber
            //   notification.PropertyType, notification.AreaSqm
            //   notification.Bedrooms, notification.Bathrooms, notification.Price
            //
            // Steps:
            //   1. Load existing active properties from your vector store / DB
            //   2. Compare using embeddings / GPT similarity
            //   3. Compute a confidence score (0.0 – 1.0)
            //   4. Set isDuplicate = true if confidence exceeds your threshold
            //
            // Example result (replace with real values):
            decimal confidence = 0m;
            bool isDuplicate = false;

            await _publisher.Publish(new PropertyDuplicateCheckCompletedEvent(
                PropertyId: notification.PropertyId,
                Confidence: confidence,
                IsDuplicate: isDuplicate
            ), cancellationToken);

            _logger.LogInformation(
                "Duplicate check completed for property {PropertyId}: confidence={Confidence}, isDuplicate={IsDuplicate}",
                notification.PropertyId, confidence, isDuplicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Duplicate detection failed for property {PropertyId}", notification.PropertyId);
            // Don't rethrow — failure here must not break the submit flow
        }
    }
}
