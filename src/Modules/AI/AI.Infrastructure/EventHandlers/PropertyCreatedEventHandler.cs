// AI.Infrastructure/EventHandlers/PropertyCreatedEventHandler.cs

using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Interfaces;
using AI.Application.Interfaces.Common;
using AI.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Property.Domain.Events;
using RealEstate.Shared.Kernel.Shared.IntegrationEvents;

namespace AI.Infrastructure.EventHandlers;

public sealed class PropertyCreatedEventHandler
    : INotificationHandler<PropertyCreatedEvent>
{
    private readonly IBlobStorage _docs;
    private readonly IOwnershipAiService _ai;
    private readonly IAiDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<PropertyCreatedEventHandler> _logger;

    public PropertyCreatedEventHandler(
        IBlobStorage docs,
        IOwnershipAiService ai,
        IAiDbContext db,
        IPublisher publisher,
        ILogger<PropertyCreatedEventHandler> logger)
    {
        _docs = docs;
        _ai = ai;
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(PropertyCreatedEvent e, CancellationToken ct)
    {
        _logger.LogInformation(
            "PropertyCreated received | PropertyId={PropertyId} OwnerUserId={UserId}",
            e.PropertyId, e.OwnerUserId);

        // 1) Create Pending verification row up-front for traceability
        var request = OwnershipVerificationRequest.CreatePending(
            e.PropertyId,
            e.OwnerUserId,
            e.OwnerName,
            e.OwnerNationalIdBlobPath,
            e.OwnershipProofBlobPath);

        _db.OwnershipVerificationRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        try
        {
            // 2) Download both documents in parallel
            var idTask = _docs.DownloadAsync(e.OwnerNationalIdBlobPath, ct);
            var proofTask = _docs.DownloadAsync(e.OwnershipProofBlobPath, ct);
            await Task.WhenAll(idTask, proofTask);

            var idDoc = await idTask;
            var proofDoc = await proofTask;

            EnsureSupportedType(idDoc);
            EnsureSupportedType(proofDoc);

            _logger.LogInformation(
                "Documents downloaded | ID={IdBytes}b ({IdType}) | Proof={ProofBytes}b ({ProofType})",
                idDoc.Content.Length, idDoc.ContentType,
                proofDoc.Content.Length, proofDoc.ContentType);

            // 3) Build location context from the event
            var expectedLocation = new PropertyLocationDto(
                Country: e.Country,
                Governorate: e.Governorate,
                City: e.City,
                District: e.District,
                AddressLine: e.AddressLine);

            // 4) Run AI pipeline (Azure DI + Claude + location scoring)
            var result = await _ai.AnalyzeAsync(
                nationalIdFile: idDoc.Content,
                nationalIdMimeType: idDoc.ContentType,
                ownershipProofFile: proofDoc.Content,
                ownershipProofMimeType: proofDoc.ContentType,
                ownerName: e.OwnerName,
                expectedLocation: expectedLocation,
                ct: ct);

            // 5) Apply result to aggregate (via Clean-Arch mapper)
            // ✅ Pass primitives directly
            request.ApplyAiResult(
                confidenceScore: result.ConfidenceScore,
                nameMatchScore: result.NameMatchScore,
                authenticityScore: result.AuthenticityScore,
                fakeDetectionFlag: result.FakeDetectionFlag,
                recommendation: result.Recommendation,
                analysisNotes: result.AnalysisNotes,
                ocrExtractedName: result.ExtractedOwnerName,
                ocrExtractedId: result.ExtractedIdExpiry,
                extractedCountry: result.ExtractedCountry,
                extractedGovernorate: result.ExtractedGovernorate,
                extractedCity: result.ExtractedCity,
                extractedDistrict: result.ExtractedDistrict,
                extractedAddress: result.ExtractedAddress,
                locationMatchScore: result.LocationMatchScore,
                locationMismatchFlag: result.LocationMismatchFlag,
                locationMatchNotes: result.LocationMatch?.Notes);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Verification completed | PropertyId={PropertyId} Status={Status} Confidence={Score} LocationMatch={Loc}",
                e.PropertyId, request.Status, result.ConfidenceScore, result.LocationMatchScore);

            // 6) Publish follow-up event
            await _publisher.Publish(new OwnershipVerificationCompletedEvent(
    PropertyId: e.PropertyId,
    RequestId: request.Id,
    Status: request.Status.ToString(),
    ConfidenceScore: result.ConfidenceScore,
    LocationMatchScore: result.LocationMatchScore,
    FakeDetectionFlag: result.FakeDetectionFlag,
    Recommendation: result.Recommendation,
    AnalysisNotes: result.AnalysisNotes,
    CompletedAt: DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Ownership verification failed | PropertyId={PropertyId}",
                e.PropertyId);

            request.MarkFailed(ex.Message);
            await _db.SaveChangesAsync(ct);

            await _publisher.Publish(new OwnershipVerificationFailedEvent(
                e.PropertyId, request.Id, ex.Message), ct);
        }
    }

    // Azure Document Intelligence accepts these formats only
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/bmp",
        "image/tiff",
        "image/heic",
        "image/heif"
    };

    private static void EnsureSupportedType(DocumentDownload doc)
    {
        if (!SupportedMimeTypes.Contains(doc.ContentType))
            throw new NotSupportedException(
                $"Unsupported document type '{doc.ContentType}' for {doc.FileName}. " +
                $"Supported: {string.Join(", ", SupportedMimeTypes)}");
    }
}