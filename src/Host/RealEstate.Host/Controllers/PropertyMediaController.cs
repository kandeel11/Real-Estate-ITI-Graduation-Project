using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Features.Queries.GetPropertyMedia;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertyMediaController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/media")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPropertyMediaQuery(id), ct));
}