using Document.Application.Commands.Rating.CreateRating;
using Document.Application.Commands.Rating.ModerateRating;
using Document.Application.Queries.Rating.GetRatingsByTarget;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public RatingsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRatingRequest req)
    {
        var id = await _mediator.Send(new CreateRatingCommand(
            req.ReviewerId, req.LeaseId, req.PropertyId,
            req.CompanyId, req.BrokerId, req.MaintenanceCompanyId,
            req.Rating, req.ReviewText));
        return Ok(new { id });
    }

    [HttpPut("{id}/moderate")]
    public async Task<IActionResult> Moderate(Guid id, [FromBody] ModerateRatingRequest req)
    {
        await _mediator.Send(new ModerateRatingCommand(id, req.ModeratedById, req.Status));
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetByTarget(
        [FromQuery] string targetType, [FromQuery] Guid targetId)
    {
        var ratings = await _mediator.Send(new GetRatingsByTargetQuery(targetType, targetId));
        return Ok(ratings);
    }
}

public record CreateRatingRequest(
    Guid ReviewerId,
    Guid? LeaseId,
    Guid? PropertyId,
    Guid? CompanyId,
    Guid? BrokerId,
    Guid? MaintenanceCompanyId,
    int Rating,
    string? ReviewText);

public record ModerateRatingRequest(
    Guid ModeratedById,
    string Status);