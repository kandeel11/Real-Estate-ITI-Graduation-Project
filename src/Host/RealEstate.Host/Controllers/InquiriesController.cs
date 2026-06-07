using Document.Application.Commands.Inquiry;
using Document.Application.DTOs;
using Document.Application.Queries.Inquiry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InquiriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InquiriesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInquiryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateInquiryCommand(
            request.PropertyId, request.TenantId, request.OwnerId, request.CompanyId, request.Message), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInquiryByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetInquiriesByPropertyQuery(propertyId), ct));

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(Guid tenantId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMyInquiriesQuery(tenantId), ct));

    [HttpPut("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptInquiryRequest request, CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new AcceptInquiryCommand(id, request.AcceptedById), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        try { await _mediator.Send(new CloseInquiryCommand(id), ct); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }
}