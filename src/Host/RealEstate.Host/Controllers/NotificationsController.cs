using Document.Application.Commands.Notification.MarkNotificationRead;
using Document.Application.Queries.Notification.GetNotificationsByUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetByUser(
        [FromQuery] Guid userId,
        [FromQuery] bool onlyUnread = false)
    {
        var notifications = await _mediator.Send(
            new GetNotificationsByUserQuery(userId, onlyUnread));
        return Ok(notifications);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id));
        return NoContent();
    }
}