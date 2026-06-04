//// AI.Infrastructure/EventHandlers/LeaseUploadedEventHandler.cs

//using AI.Application.Events;
//using AI.Application.Interfaces;
//using AI.Application.Interfaces.Common;
//using AI.Domain.Entities;
//using AI.Infrastructure.Persistence;
//using AI.Infrastructure.Persistence.Contexts;
//using Document.Domain.Events;
//using Leases.Application.Events;    // ← Yasser's event
//using MediatR;
//using Microsoft.Extensions.Logging;

//namespace AI.Infrastructure.EventHandlers;

///// <summary>
///// Mohamed subscribes to Yasser's LeaseUploadedEvent.
///// Runs OCR + GPT-4 extraction → saves result → publishes back.
///// </summary>
//public sealed class LeaseUploadedEventHandler
//    : INotificationHandler<LeaseUploadedEvent>
//{
//    private readonly ILeaseExtractionService _extraction;
//    private readonly IDocumentStorageService _storage;
//    private readonly IPublisher _publisher;
//    private readonly AiDbContext _db;
//    private readonly ILogger<LeaseUploadedEventHandler> _logger;

//    public LeaseUploadedEventHandler(
//        ILeaseExtractionService extraction,
//        IDocumentStorageService storage,
//        IPublisher publisher,
//        AiDbContext db,
//        ILogger<LeaseUploadedEventHandler> logger)
//    {
//        _extraction = extraction;
//        _storage = storage;
//        _publisher = publisher;
//        _db = db;
//        _logger = logger;
//    }

//    public async Task Handle(
//        LeaseUploadedEvent notification,
//        CancellationToken cancellationToken)
//    {
//        _logger.LogInformation(
//            "AI received LeaseUploadedEvent | LeaseId={LeaseId} | File={File}",
//            notification.LeaseId, notification.FileName);

//        // ── 1. Create extraction record in ai.lease_extraction_requests ────────
//        var extractionRequest = LeaseExtractionRequest.CreatePending(notification.LeaseId);
//        await _db.LeaseExtractionRequests.AddAsync(extractionRequest, cancellationToken);
//        await _db.SaveChangesAsync(cancellationToken);

//        try
//        {
//            // ── 2. Download PDF from Azure Blob ───────────────────────────────
//            var pdfBytes = await _storage.DownloadAsync(
//                notification.BlobPath, cancellationToken);

//            _logger.LogInformation(
//                "Downloaded PDF for lease {LeaseId} — {Size} bytes",
//                notification.LeaseId, pdfBytes.Length);

//            // ── 3. Run Azure DI OCR + Claude extraction ───────────────────────
//            var extractedData = await _extraction.ExtractAsync(
//                pdfBytes, cancellationToken);

//            _logger.LogInformation(
//                "Extraction complete | LeaseId={LeaseId} | Confidence={Score}%",
//                notification.LeaseId, extractedData.OverallConfidence);

//            // ── 4. Update extraction record ───────────────────────────────────
//            extractionRequest.MarkCompleted(
//                extractedData.OverallConfidence,
//                extractedData.RawAiResponse);

//            await _db.SaveChangesAsync(cancellationToken);

//            // ── 5. Publish result back → Yasser's handler picks it up ─────────
//            await _publisher.Publish(
//                new LeaseExtractionCompletedEvent(
//                    LeaseId: notification.LeaseId,
//                    ExtractedData: extractedData),
//                cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex,
//                "Extraction failed for LeaseId={LeaseId}", notification.LeaseId);

//            extractionRequest.MarkFailed(ex.Message);
//            await _db.SaveChangesAsync(cancellationToken);

//            // Publish failed event so Yasser can handle it too
//            await _publisher.Publish(
//                new LeaseExtractionFailedEvent(
//                    notification.LeaseId,
//                    ex.Message),
//                cancellationToken);
//        }
//    }
//}