using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Document.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for live notifications. Every authenticated user automatically
/// joins their personal group "notification-{userId}" on connect, so the
/// NotificationService can push to them by user id.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value
                  ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"notification-{userId}");

        await base.OnConnectedAsync();
    }
}
