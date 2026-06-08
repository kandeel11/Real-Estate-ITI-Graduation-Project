using Document.Application.Commands.Notification;
using Document.Application.Queries.Notification;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetNotificationsQuery(UserId, unreadOnly, page, pageSize), ct));

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        => Ok(new { count = await sender.Send(new GetUnreadNotificationCountQuery(UserId), ct) });

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await sender.Send(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await sender.Send(new MarkAllNotificationsReadCommand(UserId), ct);
        return NoContent();
    }
}