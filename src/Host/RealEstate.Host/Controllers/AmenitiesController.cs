using MediatR;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetAllAmenities;
using Property.Application.Features.Queries.GetPropertyAmenities;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/amenities")]
public class AmenitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AmenitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAllAmenitiesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPropertyAmenitiesQuery(propertyId), ct);
        return Ok(result);
    }
}
