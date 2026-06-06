// AI.Presentation/Controllers/SmartSearchController.cs

using AI.Application.Commands.DeleteSavedSearch;
using AI.Application.Commands.SaveSearch;
using AI.Application.Commands.SmartSearch;
using AI.Application.DTOs;
using AI.Application.Queries.GetSavedSearches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Presentation.Controllers;

[ApiController]
[Route("api/search")]
[Produces("application/json")]
public sealed class SmartSearchController : ControllerBase
{
    private readonly ISender _sender;

    public SmartSearchController(ISender sender) => _sender = sender;

    /// <summary>
    /// Natural language property search.
    /// Supports Arabic, English, and mixed queries.
    /// Available to ALL plans.
    /// </summary>
    [HttpPost("smart")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SmartSearchResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SmartSearchAsync(
        [FromBody] SmartSearchInputRequest req,
        CancellationToken ct)
    {
        var result = await _sender.Send(new SmartSearchCommand(
            //UserId: User.Identity?.IsAuthenticated == true
            //          ? "019e9e53-21f8-7d70-9865-fb1c7b75f6a5" : null,
            UserId: Guid.Parse("019e9e53-21f8-7d70-9865-fb1c7b75f6a5"),
            RawQuery: req.Query,
            Page: req.Page,
            PageSize: req.PageSize), ct);

        return Ok(result);
    }

    /// <summary>
    /// Save a search query with optional new-property notifications.
    /// Premium plans only.
    /// </summary>
    [HttpPost("save")]
    [Authorize]
    [ProducesResponseType(typeof(SavedSearchDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SaveSearchAsync(
        [FromBody] SaveSearchInputRequest req,
        CancellationToken ct)
    {
        var result = await _sender.Send(new SaveSearchCommand(
            UserId: Guid.Parse("019e9e53-21f8-7d70-9865-fb1c7b75f6a5"),
            QueryId: req.QueryId,
            Name: req.Name,
            NotifyOnMatch: req.NotifyOnMatch), ct);

        return CreatedAtAction(nameof(GetSavedSearchesAsync), result);
    }

    /// <summary>
    /// Get all saved searches for the authenticated user.
    /// </summary>
    [HttpGet("saved")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<SavedSearchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedSearchesAsync(CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetSavedSearchesQuery(Guid.Parse("019e9e53-21f8-7d70-9865-fb1c7b75f6a5")), ct);

        return Ok(result);
    }

    /// <summary>
    /// Delete (deactivate) a saved search.
    /// </summary>
    [HttpDelete("saved/{savedSearchId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSavedSearchAsync(
        Guid savedSearchId, CancellationToken ct)
    {
        await _sender.Send(new DeleteSavedSearchCommand(
            SavedSearchId: savedSearchId,
            UserId: Guid.Parse("019e9e53-21f8-7d70-9865-fb1c7b75f6a5")), ct);

        return NoContent();
    }
}

// ── Request models ────────────────────────────────────────────────────────────
public sealed record SmartSearchInputRequest(
    string Query,
    int Page = 1,
    int PageSize = 20
);

public sealed record SaveSearchInputRequest(
    Guid QueryId,
    string Name,
    bool NotifyOnMatch
);