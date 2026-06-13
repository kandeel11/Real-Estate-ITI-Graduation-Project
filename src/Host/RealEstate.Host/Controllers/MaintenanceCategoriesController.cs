using MaintenanceCompanies.Application.Commands.CreateCategory;
using MaintenanceCompanies.Application.Queries.Categories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/maintenance-categories")]
public class MaintenanceCategoriesController(IMediator mediator) : ControllerBase
{
    // GET /api/maintenance-categories?includeInactive=false
    // عام عشان الـ AI والـ tenant يجيبوا الـ categories المفعّلة
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive,
        CancellationToken ct)
    {
        var categories = await mediator.Send(new GetCategoriesQuery(includeInactive), ct);
        return Ok(categories);
    }

    // GET /api/maintenance-categories/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var category = await mediator.Send(new GetCategoryByIdQuery(id), ct);

        return category == null
            ? NotFound(new { message = "Category not found." })
            : Ok(category);
    }

    // POST /api/maintenance-categories  (Admin بس)
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken ct)
    {
        try
        {
            var categoryDto = await mediator.Send(command, ct);

            // بيرجّع 201 + Location لـ GetById
            return CreatedAtAction(nameof(GetById),
                new { id = categoryDto.Id },
                new { Category = categoryDto, Message = "Category created successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}