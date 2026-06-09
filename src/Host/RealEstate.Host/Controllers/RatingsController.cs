using Document.Application.Commands.Rating;
using Document.Application.DTOs;
using Document.Application.Queries.Rating;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Document.Application.Commands.Rating.CreateRating;
using Document.Application.Commands.Rating.ModerateRating;
using Document.Application.Queries.Rating.GetRatingsByTarget;
using Microsoft.AspNetCore.Mvc;
using CreateRatingCommand = Document.Application.Commands.Rating.CreateRatingCommand;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpPost]
    [Authorize(Roles = "tenant")]
    public async Task<IActionResult> Create([FromBody] CreateRatingRequest req, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new CreateRatingCommand(
                UserId, req.LeaseId, req.PropertyId,
                req.MaintenanceCompanyId, req.RatingValue, req.ReviewText), ct);
            return CreatedAtAction(nameof(GetMine), result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRatingRequest req, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new UpdateRatingCommand(id, UserId, req.RatingValue, req.ReviewText), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await sender.Send(new DeleteRatingCommand(id, UserId), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await sender.Send(new GetMyRatingsQuery(UserId), ct));

    [HttpGet("property/{propertyId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForProperty(Guid propertyId, CancellationToken ct)
        => Ok(await sender.Send(new GetPropertyRatingsQuery(propertyId), ct));
}
