using Identity.Application.Interfaces;
using MediatR;
using RealEstate.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Commands.AddPropertyAmenity;
using Property.Application.Features.Commands.AssignBroker;
using Property.Application.Features.Commands.MarkAsRented;
using Property.Application.Features.Commands.RequestAiDescription;
using Property.Application.Features.Commands.AddPropertyMedia;
using Property.Application.Features.Commands.CreateProperty;
using Property.Application.Features.Commands.DeleteProperty;
using Property.Application.Features.Commands.RemovePropertyAmenity;
using Property.Application.Features.Commands.RemovePropertyMedia;
using Property.Application.Features.Commands.SetAiDescription;
using Property.Application.Features.Commands.SetAiDuplicateScore;
using Property.Application.Features.Commands.SetFeatured;
using Property.Application.Features.Commands.SetPrimaryMedia;
using Property.Application.Features.Commands.SetVerificationStatus;
using Property.Application.Features.Commands.SubmitPropertyForReview;
using Property.Application.Features.Commands.TrackPropertyView;
using Property.Application.Features.Commands.UpdateProperty;
using Property.Application.Features.Queries.GetAllProperties;
using Property.Application.Features.Queries.GetFeaturedProperties;
using Property.Application.Features.Queries.GetPendingVerification;
using Property.Application.Features.Queries.GetPropertiesByOwner;
using Property.Application.Features.Queries.GetPropertyAnalytics;
using Property.Application.Features.Queries.GetPropertyById;
using Property.Application.Features.Queries.SearchProperties;
using Property.Domain.Enums;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly PropertyEnrichmentService _enrichment;

    public PropertiesController(IMediator mediator, ICurrentUserService currentUser, PropertyEnrichmentService enrichment)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _enrichment = enrichment;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllPropertiesQuery(), ct);
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
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchPropertiesQuery(
            city, district, propertyType, minPrice, maxPrice,
            minBedrooms, maxBedrooms, bedrooms, isFurnished, petsAllowed,
            latitude, longitude, radiusKm, page, pageSize), ct);
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

    [HttpPost]
    [Authorize(Policy = "OwnerOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct = default)
    {
        var ownerId = _currentUser.UserId;
        if (ownerId is null) return Unauthorized(new { error = "User not authenticated." });

        var id = await _mediator.Send(new CreatePropertyCommand(
            ownerId.Value,
            request.CompanyId ?? _currentUser.CompanyId,
            request.ListingBrokerId,
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
            request.Latitude,
            request.Longitude,
            request.Address,
            request.ApartmentNumber,
            request.City,
            request.District,
            request.ListingSource), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
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
            request.Latitude,
            request.Longitude,
            request.Address,
            request.ApartmentNumber,
            request.City,
            request.District,
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SetFeatured(Guid id, [FromBody] SetFeaturedRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized(new { error = "User not authenticated." });

        await _mediator.Send(new SetFeaturedCommand(id, userId.Value, request.IsFeatured, request.FeaturedUntil), ct);
        return NoContent();
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

        await _mediator.Send(new PropertyAiDescriptionRequestCommand(id, userId.Value), ct);
        return Accepted(new { message = "AI description is being generated. It will appear shortly.", propertyId = id });
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

public record CreatePropertyRequest(
    Guid? CompanyId,
    Guid? ListingBrokerId,
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
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? ApartmentNumber,
    string City,
    string District,
    ListingSource ListingSource
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
    decimal Latitude,
    decimal Longitude,
    string Address,
    int? ApartmentNumber,
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
