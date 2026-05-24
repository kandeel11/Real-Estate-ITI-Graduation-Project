using MediatR;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Commands.AddPropertyAmenity;
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
using Property.Application.Features.Commands.SyndicateProperty;
using Property.Application.Features.Commands.TrackPropertyView;
using Property.Application.Features.Commands.UpdateProperty;
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

    public PropertiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] string? propertyType,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? maxBedrooms,
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
            minBedrooms, maxBedrooms, isFurnished, petsAllowed,
            latitude, longitude, radiusKm, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeaturedPropertiesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("pending-verification")]
    public async Task<IActionResult> GetPendingVerification(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPendingVerificationQuery(), ct);
        return Ok(result);
    }

    [HttpGet("owner/{ownerId:guid}")]
    public async Task<IActionResult> GetByOwner(Guid ownerId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPropertiesByOwnerQuery(ownerId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        // Track the view asynchronously — fire and forget pattern with no IP here; tracking is best-effort
        _ = _mediator.Send(new TrackPropertyViewCommand(id, null, null, ViewSource.Direct), ct);

        var result = await _mediator.Send(new GetPropertyByIdQuery(id), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct = default)
    {
        var id = await _mediator.Send(new CreatePropertyCommand(
            request.OwnerId,
            request.CompanyId,
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
            request.City,
            request.District,
            request.ListingSource), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new UpdatePropertyCommand(
            id,
            request.RequestingUserId,
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
            request.City,
            request.District,
            request.ListingBrokerId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeletePropertyRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new DeletePropertyCommand(id, request.RequestingUserId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] OwnerRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SubmitPropertyForReviewCommand(id, request.RequestingUserId), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/verification")]
    public async Task<IActionResult> SetVerification(Guid id, [FromBody] SetVerificationRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetVerificationStatusCommand(id, request.VerificationStatus, request.RejectionReason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> SetFeatured(Guid id, [FromBody] SetFeaturedRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetFeaturedCommand(id, request.RequestingUserId, request.IsFeatured, request.FeaturedUntil), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/syndicate")]
    public async Task<IActionResult> Syndicate(Guid id, [FromBody] OwnerRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SyndicatePropertyCommand(id, request.RequestingUserId), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/ai-duplicate-score")]
    public async Task<IActionResult> SetAiDuplicateScore(Guid id, [FromBody] SetAiDuplicateScoreRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetAiDuplicateScoreCommand(id, request.Confidence), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/ai-description")]
    public async Task<IActionResult> SetAiDescription(Guid id, [FromBody] SetAiDescriptionRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetAiDescriptionCommand(id, request.DescriptionEn, request.DescriptionAr), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> GetAnalytics(Guid id, [FromQuery] Guid requestingUserId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPropertyAnalyticsQuery(id, requestingUserId), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/media")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadMedia(
        Guid id,
        [FromForm] UploadMediaRequest request,
        CancellationToken ct = default)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest("No file provided.");

        await using var stream = request.File.OpenReadStream();
        var mediaId = await _mediator.Send(new AddPropertyMediaCommand(
            id,
            request.RequestingUserId,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.MediaType,
            (int)(request.File.Length / 1024),
            request.SortOrder), ct);

        return Ok(new { mediaId });
    }

    [HttpPut("{id:guid}/media/{mediaId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryMedia(Guid id, Guid mediaId, [FromBody] OwnerRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new SetPrimaryMediaCommand(id, mediaId, request.RequestingUserId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/media/{mediaId:guid}")]
    public async Task<IActionResult> RemoveMedia(Guid id, Guid mediaId, [FromQuery] Guid requestingUserId, CancellationToken ct = default)
    {
        await _mediator.Send(new RemovePropertyMediaCommand(id, mediaId, requestingUserId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/amenities")]
    public async Task<IActionResult> AddAmenity(Guid id, [FromBody] AddAmenityRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new AddPropertyAmenityCommand(id, request.AmenityId, request.RequestingUserId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/amenities/{amenityId:guid}")]
    public async Task<IActionResult> RemoveAmenity(Guid id, Guid amenityId, [FromQuery] Guid requestingUserId, CancellationToken ct = default)
    {
        await _mediator.Send(new RemovePropertyAmenityCommand(id, amenityId, requestingUserId), ct);
        return NoContent();
    }
}

// --- Request DTOs ---

public record CreatePropertyRequest(
    Guid OwnerId,
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
    string City,
    string District,
    ListingSource ListingSource
);

public record UpdatePropertyRequest(
    Guid RequestingUserId,
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
    string City,
    string District,
    Guid? ListingBrokerId
);

public record DeletePropertyRequest(Guid RequestingUserId);

public record OwnerRequest(Guid RequestingUserId);

public record SetVerificationRequest(VerificationStatus VerificationStatus, string? RejectionReason);

public record SetFeaturedRequest(Guid RequestingUserId, bool IsFeatured, DateTime? FeaturedUntil);

public record SetAiDuplicateScoreRequest(decimal Confidence);

public record SetAiDescriptionRequest(string? DescriptionEn, string? DescriptionAr);

public record AddAmenityRequest(Guid AmenityId, Guid RequestingUserId);

public class UploadMediaRequest
{
    public IFormFile? File { get; init; }
    public Guid RequestingUserId { get; init; }
    public MediaType MediaType { get; init; }
    public int SortOrder { get; init; }
}
