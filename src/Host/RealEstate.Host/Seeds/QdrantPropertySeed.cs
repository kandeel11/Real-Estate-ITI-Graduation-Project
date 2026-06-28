using AI.Application.DTOs;
using AI.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Infrastructure.Persistence.Contexts;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RealEstate.Host.Seeds;

/// <summary>
/// Uploads the demo-seeded properties into the Qdrant 'properties' collection
/// so they participate in duplicate detection from the very first request.
///
/// Idempotent: checks which property IDs are missing before generating embeddings,
/// so re-runs on startup are cheap when all points already exist.
///
/// Wrapped in try-catch so a missing/unreachable Qdrant instance never blocks startup.
/// </summary>
public static class QdrantPropertySeed
{
    private const string CollectionName = "properties";
    private const int    EmbeddingDimension = 1536;

    public static async Task SeedAsync(
        PropertyDbContext    propertyDb,
        IdentityDbContext    identityDb,
        IPropertyDuplicateCheckService duplicateCheck,
        QdrantClient        qdrant,
        ILogger             logger,
        CancellationToken   ct = default)
    {
        try
        {
            await RunAsync(propertyDb, identityDb, duplicateCheck, qdrant, logger, ct);
        }
        catch (Exception ex)
        {
            // Never block startup when Qdrant is unreachable or embedding service is unconfigured.
            logger.LogWarning(ex,
                "QdrantPropertySeed: failed — Qdrant may be unavailable or embeddings unconfigured. " +
                "App will continue without pre-seeded vectors.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static async Task RunAsync(
        PropertyDbContext    propertyDb,
        IdentityDbContext    identityDb,
        IPropertyDuplicateCheckService duplicateCheck,
        QdrantClient        qdrant,
        ILogger             logger,
        CancellationToken   ct)
    {
        // 1. Resolve seeded owner IDs via known seed emails
        var seedUserIds = await identityDb.Users
            .Where(u => PropertyDataSeed.SeedEmails.Contains(u.Email))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (seedUserIds.Count == 0)
        {
            logger.LogInformation("QdrantPropertySeed: no seeded users found — skipping.");
            return;
        }

        // 2. Fetch seeded properties from SQL
        var properties = await propertyDb.Properties
            .Where(p => seedUserIds.Contains(p.OwnerId) && p.DeletedAt == null)
            .ToListAsync(ct);

        if (properties.Count == 0)
        {
            logger.LogInformation("QdrantPropertySeed: no seeded properties found — skipping.");
            return;
        }

        // 3. Ensure collection + payload indexes exist
        await EnsureCollectionAsync(qdrant, logger, ct);

        // 4. Determine which IDs are already in Qdrant (idempotency)
        var pointIds = properties
            .Select(p => new PointId { Uuid = p.Id.ToString() })
            .ToList();

        var existing = await qdrant.RetrieveAsync(
            CollectionName,
            pointIds,
            withPayload: false,
            withVectors: false,
            cancellationToken: ct);

        var existingUuids = existing
            .Select(pt => pt.Id.Uuid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = properties
            .Where(p => !existingUuids.Contains(p.Id.ToString()))
            .ToList();

        if (missing.Count == 0)
        {
            logger.LogInformation(
                "QdrantPropertySeed: all {Total} seeded properties already in Qdrant — nothing to do.",
                properties.Count);
            return;
        }

        logger.LogInformation(
            "QdrantPropertySeed: uploading {Missing} of {Total} seeded properties…",
            missing.Count, properties.Count);

        // 5. Embed + upsert each missing property
        foreach (var prop in missing)
        {
            var request = new PropertyDuplicateCheckRequest(
                PropertyId:   prop.Id,
                OwnerId:      prop.OwnerId,
                Title:        prop.Title,
                Description:  prop.Description,
                City:         prop.City,
                District:     prop.District,
                Street:       prop.Address,
                BuildingName: null,
                UnitNumber:   prop.ApartmentNumber?.ToString(),
                Floor:        prop.FloorNumber,
                PropertyType: prop.PropertyType.ToString(),
                ListingType:  "ForRent",
                AreaInMeters: (double)prop.AreaSqm,
                Bedrooms:     prop.Bedrooms,
                Bathrooms:    prop.Bathrooms,
                Price:        prop.Price);

            await duplicateCheck.UpsertDirectAsync(request, ct);

            logger.LogDebug(
                "QdrantPropertySeed: upserted {Id} — {Title} ({City}/{District})",
                prop.Id, prop.Title, prop.City, prop.District);
        }

        logger.LogInformation(
            "QdrantPropertySeed: done — {Count} propert{Suffix} upserted.",
            missing.Count, missing.Count == 1 ? "y" : "ies");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Creates the 'properties' collection and its payload indexes when absent.
    // Swallows concurrent-creation race conditions gracefully.
    // ─────────────────────────────────────────────────────────────────────────
    private static async Task EnsureCollectionAsync(
        QdrantClient      qdrant,
        ILogger           logger,
        CancellationToken ct)
    {
        try
        {
            if (await qdrant.CollectionExistsAsync(CollectionName, ct))
                return;

            logger.LogInformation("QdrantPropertySeed: creating '{Collection}' collection…", CollectionName);

            await qdrant.CreateCollectionAsync(
                CollectionName,
                new VectorParams { Size = EmbeddingDimension, Distance = Distance.Cosine },
                cancellationToken: ct);

            // Payload indexes for fast filtered search
            await qdrant.CreatePayloadIndexAsync(CollectionName, "city",         PayloadSchemaType.Keyword, cancellationToken: ct);
            await qdrant.CreatePayloadIndexAsync(CollectionName, "district",     PayloadSchemaType.Keyword, cancellationToken: ct);
            await qdrant.CreatePayloadIndexAsync(CollectionName, "propertyType", PayloadSchemaType.Keyword, cancellationToken: ct);
            await qdrant.CreatePayloadIndexAsync(CollectionName, "listingType",  PayloadSchemaType.Keyword, cancellationToken: ct);
            await qdrant.CreatePayloadIndexAsync(CollectionName, "ownerId",      PayloadSchemaType.Keyword, cancellationToken: ct);

            logger.LogInformation(
                "QdrantPropertySeed: collection '{Collection}' created with 5 keyword indexes.",
                CollectionName);
        }
        catch (Exception ex)
        {
            // Another instance may have created it concurrently — not an error.
            logger.LogWarning(ex,
                "QdrantPropertySeed: collection creation threw (may have been created concurrently).");
        }
    }
}
