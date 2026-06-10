using AI.Application.Commands.DeleteSavedSearch;
using AI.Application.Commands.SaveSearch;
using AI.Application.Commands.SmartSearch;
using AI.Application.DTOs;
using AI.Application.Queries.GetSavedSearches;
using Identity.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Presentation.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SmartSearchController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("smart")]
    [AllowAnonymous]
    public async Task<IActionResult> SmartSearchAsync([FromBody] SmartSearchInputRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new SmartSearchCommand(
            UserId: currentUser.IsAuthenticated ? currentUser.UserId : null,
            RawQuery: req.Query,
            Page: req.Page,
            PageSize: req.PageSize), ct);
        return Ok(result);
    }

    [HttpPost("save")]
    [Authorize]
    public async Task<IActionResult> SaveSearchAsync([FromBody] SaveSearchInputRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await sender.Send(new SaveSearchCommand(userId, req.QueryId, req.Name, req.NotifyOnMatch), ct);
        return CreatedAtAction(nameof(GetSavedSearchesAsync), result);
    }

    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSavedSearchesAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return Ok(await sender.Send(new GetSavedSearchesQuery(userId), ct));
    }

    [HttpDelete("saved/{savedSearchId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSavedSearchAsync(Guid savedSearchId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        await sender.Send(new DeleteSavedSearchCommand(savedSearchId, userId), ct);
        return NoContent();
    }
}

public sealed record SmartSearchInputRequest(string Query, int Page = 1, int PageSize = 20);
public sealed record SaveSearchInputRequest(Guid QueryId, string Name, bool NotifyOnMatch);