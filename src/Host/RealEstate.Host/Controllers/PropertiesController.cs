using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Commands.AddPropertyAmenity;
using Property.Application.Features.Commands.AddPropertyMedia;
using Property.Application.Features.Commands.AssignBroker;
using Property.Application.Features.Commands.CreateProperty;
using Property.Application.Features.Commands.DeleteProperty;
using Property.Application.Features.Commands.MarkAsRented;
using Property.Application.Features.Commands.RemovePropertyAmenity;
using Property.Application.Features.Commands.RemovePropertyMedia;
using Property.Application.Features.Commands.RequestAiDescription;
using Property.Application.Features.Commands.SetAiDescription;
using Property.Application.Features.Commands.SetAiDuplicateScore;
using Property.Application.Features.Commands.SetFeatured;
using Property.Application.Features.Commands.SetPrimaryMedia;
using Property.Application.Features.Commands.SetVerificationStatus;
using Property.Application.Features.Commands.SubmitPropertyForReview;
using Property.Application.Features.Commands.TrackPropertyView;
using Property.Application.Features.Commands.UpdateProperty;
using Property.Application.Features.Queries.GetActiveProperties;
using Property.Application.Features.Queries.GetAllProperties;
using Property.Application.Features.Queries.GetFeaturedProperties;
using Property.Application.Features.Queries.GetPendingVerification;
using Property.Application.Features.Queries.GetPropertiesByOwner;
using Property.Application.Features.Queries.GetPropertyAnalytics;
using Property.Application.Features.Queries.GetPropertyById;
using Property.Application.Features.Queries.SearchProperties;
using Property.Application.Interfaces;
using Property.Domain.Enums;
using RealEstate.Host.Services;
using static Property.Application.Interfaces.IMediaStorageService;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly PropertyEnrichmentService _enrichment;
    private readonly IMediaStorageService _docs;

    public PropertiesController(IMediator mediator, ICurrentUserService currentUser, PropertyEnrichmentService enrichment, IMediaStorageService docs)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _enrichment = enrichment;
        _docs = docs;
    }

    // Admin-only: returns ALL properties including Draft/Rejected. Public callers should
    // use GET /api/properties/active or /search (which only expose Active listings).
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllPropertiesQuery(), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetActivePropertiesQuery(), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] string? propertyType,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? maxBedrooms,
        [FromQuery] int? bedrooms,
        [FromQuery] bool? isFurnished,
        [FromQuery] bool? petsAllowed,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchPropertiesQuery(
            city, district, propertyType, minPrice, maxPrice,
            minBedrooms, maxBedrooms, bedrooms, isFurnished, petsAllowed,
            page, pageSize), ct);
        await _enrichment.EnrichAsync(result.Items, ct);
        return Ok(result);
    }

    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedPropertiesQuery(), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("pending-verification")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPendingVerification(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPendingVerificationQuery(), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("owner/{ownerId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetByOwner(Guid ownerId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPropertiesByOwnerQuery(ownerId), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });
        var result = await _mediator.Send(new GetPropertiesByOwnerQuery(userId.Value), ct);
        await _enrichment.EnrichAsync(result, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPropertyByIdQuery(id), ct);
        if (result is null) return NotFound();

        await _enrichment.EnrichAsync(result, ct);
        await _mediator.Send(new TrackPropertyViewCommand(id, null, null, ViewSource.Direct), ct);

        return Ok(result);
    }

    // Properties.Api/Controllers/PropertiesController.cs

    [HttpPost]
    [Authorize(Policy = "OwnerOrAdmin")]
    [Consumes("multipart/form-data")]                                    // 🆕 multipart
    [RequestSizeLimit(50 * 1024 * 1024)]                                 // 50 MB cap
    public async Task<IActionResult> Create(
        [FromForm] CreatePropertyRequest request,
        CancellationToken ct = default)
    {
        var ownerId = _currentUser.UserId;
        if (ownerId is null)
            return Unauthorized(new { error = "User not authenticated." });

        // Company managers don't create listings themselves — their brokers do.
        // (A manager-created listing would have no owner identity to verify and wouldn't
        //  belong to a broker, so we block it and steer them to add/assign a broker.)
        if (_currentUser.IsInRole("company_manager"))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Company managers cannot create listings directly. Add a broker to your company — brokers create and are assigned listings."
            });

        // 🔍 Determine submission type from role
        var isOwnerSubmission = _currentUser.IsInRole("owner");          // ⬅ adjust to your role constants
        var isBrokerSubmission = _currentUser.IsInRole("broker");

        // CompanyId / ListingBrokerId are derived from the caller's identity — never from the body:
        //   • Owner            → both null
        //   • Individual broker → CompanyId null,            ListingBrokerId = broker's own user id
        //   • Company broker    → CompanyId = broker's company, ListingBrokerId = broker's own user id
        Guid? companyId = null;
        Guid? listingBrokerId = null;
        if (isBrokerSubmission)
        {
            listingBrokerId = ownerId.Value;          // ListingBrokerId is the broker's USER id
            companyId = _currentUser.CompanyId;       // null for individual broker, set for company broker
        }

        string? nationalIdBlobPath = null;
        string? ownershipProofBlobPath = null;

        if (isOwnerSubmission)
        {
            // ── Owner path: REQUIRE both files + owner name ──────────
            if (request.NationalIdFile is null || request.NationalIdFile.Length == 0)
                return BadRequest(new { error = "National ID file is required for owner submissions." });

            if (request.OwnershipProofFile is null || request.OwnershipProofFile.Length == 0)
                return BadRequest(new { error = "Ownership proof file is required for owner submissions." });

            if (string.IsNullOrWhiteSpace(request.OwnerFullName))
                return BadRequest(new { error = "Owner full name is required." });

            // Validate file types
            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/bmp", "image/tiff" };
            if (!allowedTypes.Contains(request.NationalIdFile.ContentType) ||
                !allowedTypes.Contains(request.OwnershipProofFile.ContentType))
                return BadRequest(new { error = "Documents must be PDF or image (JPEG/PNG/BMP/TIFF)." });

            // Upload to blob storage in parallel
            var folder = $"ownership/{ownerId.Value}";
            var idTask = UploadAsync(_currentUser.UserId, request.NationalIdFile, MediaCategory.Ownership, ct);
            var proofTask = UploadAsync(_currentUser.UserId, request.OwnershipProofFile, MediaCategory.Ownership, ct);
            await Task.WhenAll(idTask, proofTask);

            nationalIdBlobPath = await idTask;
            ownershipProofBlobPath = await proofTask;
        }
        else if (isBrokerSubmission)
        {
            // ── Broker path: files NOT required, skip verification ───
            // (blob paths stay null → handler skips PropertyCreatedEvent)
        }

        // ── Build & send command ─────────────────────────────────────
        try
        {
            var id = await _mediator.Send(new CreatePropertyCommand(
       OwnerId: ownerId.Value,
       CompanyId: companyId,
       ListingBrokerId: listingBrokerId,
       Title: request.Title,
       TitleAr: request.TitleAr,
       // Descriptions come from the AI flow later; start empty.
       Description: string.Empty,
       DescriptionAr: string.Empty,
       PropertyType: request.PropertyType,
       Price: request.Price,
       Bedrooms: request.Bedrooms,
       Bathrooms: request.Bathrooms,
       AreaSqm: request.AreaSqm,
       FloorNumber: request.FloorNumber,
       TotalFloors: request.TotalFloors,
       YearBuilt: request.YearBuilt,
       IsFurnished: request.IsFurnished,
       PetsAllowed: request.PetsAllowed,
       Address: request.Address,
       ApartmentNumber: request.ApartmentNumber,
       Country: request.Country,
       Governorate: request.Governorate,

       // 👇 هذا هو السطر الذي كان ينقصك لحل المشكلة
       ownerShipStatus: "Pending", // يمكنك تغييره إلى request.OwnerShipStatus إذا كان قادماً من الـ Request

       City: request.City,
       District: request.District,
       ListingSource: request.ListingSource,
       IsOwnerSubmission: isOwnerSubmission,
       OwnerFullName: request.OwnerFullName,
       NationalIdBlobPath: nationalIdBlobPath,
       OwnershipProofBlobPath: ownershipProofBlobPath), ct);

            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            // Duplicate listing (same owner+title, or same address+apartment) or plan limit reached.
            // The property is NOT created in any of these cases.
            return Conflict(new { error = ex.Message });
        }
    }

    // ─── Helper for upload ────────────────────────────────────────────
    // ─── Helper for ownership document upload ─────────────────────────
    private async Task<string> UploadAsync(
        Guid? OwnerId,
        IFormFile file,
        MediaCategory category,
        CancellationToken ct)
    {
        using var stream = file.OpenReadStream();

        return await _docs.UploadAsync(
            OwnerId: _currentUser.UserId,
            fileStream: stream,
            fileName: file.FileName,
            contentType: file.ContentType,
            category: category,
            ct: ct);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "OwnerOrAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new UpdatePropertyCommand(
            id,
            userId.Value,
            request.Title,
            request.TitleAr,
            request.Description,
            request.DescriptionAr,
            request.PropertyType,
            request.Price,
            request.Bedrooms,
            request.Bathrooms,
            request.AreaSqm,
            request.FloorNumber,
            request.TotalFloors,
            request.YearBuilt,
            request.IsFurnished,
            request.PetsAllowed,
            request.Address,
            request.ApartmentNumber,
            request.City,
            request.District,
            request.Country,
            request.Governorate,
            request.ListingBrokerId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "OwnerOrAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new DeletePropertyCommand(id, userId.Value), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-rented")]
    [Authorize(Policy = "OwnerOrAdmin")]
    public async Task<IActionResult> MarkAsRented(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await _mediator.Send(new MarkAsRentedCommand(id, userId.Value), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new SubmitPropertyForReviewCommand(id, userId.Value), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/verification")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetVerification(Guid id, [FromBody] SetVerificationRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetVerificationStatusCommand(id, request.VerificationStatus, request.RejectionReason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/feature")]
    [Authorize]
    public async Task<IActionResult> SetFeatured(Guid id, [FromBody] SetFeaturedRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await _mediator.Send(
                new SetFeaturedCommand(id, userId.Value, _currentUser.CompanyId, request.IsFeatured, request.FeaturedUntil), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/ai-duplicate-score")]
    [Authorize(Policy = "InternalServiceOnly")]
    public async Task<IActionResult> SetAiDuplicateScore(Guid id, [FromBody] SetAiDuplicateScoreRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetAiDuplicateScoreCommand(id, request.Confidence), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/assign-broker")]
    [Authorize(Policy = "CompanyManager")]
    public async Task<IActionResult> AssignBroker(Guid id, [FromBody] AssignBrokerRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await _mediator.Send(new AssignBrokerToPropertyCommand(
                PropertyId: id,
                RequestingUserId: userId.Value,
                RequestingCompanyId: _currentUser.CompanyId,
                BrokerId: request.BrokerId), ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/request-ai-description")]
    [Authorize]
    public async Task<IActionResult> RequestAiDescription(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        try
        {
            await _mediator.Send(new PropertyAiDescriptionRequestCommand(id, userId.Value), ct);
            return Accepted(new { message = "AI description is being generated. It will appear shortly.", propertyId = id });
        }
        catch (InvalidOperationException ex)
        {
            // Plan AI quota reached
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/ai-description")]
    [Authorize(Policy = "InternalServiceOnly")]
    public async Task<IActionResult> SetAiDescription(Guid id, [FromBody] SetAiDescriptionRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetAiDescriptionCommand(id, request.DescriptionEn, request.DescriptionAr), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/analytics")]
    [Authorize]
    public async Task<IActionResult> GetAnalytics(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        var result = await _mediator.Send(new GetPropertyAnalyticsQuery(id, userId.Value), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/media")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia(
        Guid id,
        [FromForm] UploadMediaRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        if (request.File is null || request.File.Length == 0)
            return BadRequest("No file provided.");

        try
        {
            await using var stream = request.File.OpenReadStream();
            var mediaId = await _mediator.Send(new AddPropertyMediaCommand(
                id,
                userId.Value,
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.MediaType,
                (int)(request.File.Length / 1024),
                request.SortOrder), ct);

            return Ok(new { mediaId });
        }
        catch (InvalidOperationException ex)
        {
            // Plan storage limit reached
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}/media/{mediaId:guid}/primary")]
    [Authorize]
    public async Task<IActionResult> SetPrimaryMedia(Guid id, Guid mediaId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new SetPrimaryMediaCommand(id, mediaId, userId.Value), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/media/{mediaId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveMedia(Guid id, Guid mediaId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new RemovePropertyMediaCommand(id, mediaId, userId.Value), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/amenities")]
    [Authorize]
    public async Task<IActionResult> AddAmenity(Guid id, [FromBody] AddAmenityRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new AddPropertyAmenityCommand(id, request.AmenityId, userId.Value), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/amenities/{amenityId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveAmenity(Guid id, Guid amenityId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new RemovePropertyAmenityCommand(id, amenityId, userId.Value), ct);
        return NoContent();
    }
}

// --- Request DTOs ---

// Property.Application/Features/Commands/CreateProperty/CreatePropertyRequest.cs



public record CreatePropertyRequest(
    // CompanyId & ListingBrokerId are NOT accepted from the client — they're derived from the
    // caller's JWT (role + company_id) in the controller. See Create().
    string Title,
    string TitleAr,
    // Description/DescriptionAr are NOT collected here — they are produced by the AI
    // description flow (request-ai-description → accept) after the property exists.
    PropertyType PropertyType,
    decimal Price,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    int FloorNumber,
    int TotalFloors,
    int YearBuilt,
    bool IsFurnished,
    bool PetsAllowed,
    string Address,
    int? ApartmentNumber,
    string Country,            // 🆕 needed for location verification
    string Governorate,        // 🆕 needed for location verification
    string City,
    string District,
    ListingSource ListingSource,

    // 🆕 Ownership documents (only required when owner submits)
    string? OwnerFullName,
    IFormFile? NationalIdFile,
    IFormFile? OwnershipProofFile
);
public record UpdatePropertyRequest(
    string Title,
    string TitleAr,
    string Description,
    string DescriptionAr,
    PropertyType PropertyType,
    decimal Price,
    int Bedrooms,
    int Bathrooms,
    decimal AreaSqm,
    int FloorNumber,
    int TotalFloors,
    int YearBuilt,
    bool IsFurnished,
    bool PetsAllowed,
    string Address,
    int? ApartmentNumber,
    string Country,            // 🆕
    string Governorate,        // 🆕
    string City,
    string District,
    Guid? ListingBrokerId
);
public record SetVerificationRequest(VerificationStatus VerificationStatus, string? RejectionReason);

public record SetFeaturedRequest(bool IsFeatured, DateTime? FeaturedUntil);

public record SetAiDuplicateScoreRequest(decimal Confidence);

public record SetAiDescriptionRequest(string? DescriptionEn, string? DescriptionAr);

public record AssignBrokerRequest(Guid? BrokerId);

public record AddAmenityRequest(Guid AmenityId);

public class UploadMediaRequest
{
    public IFormFile? File { get; init; }
    public MediaType MediaType { get; init; }
    public int SortOrder { get; init; }
}
