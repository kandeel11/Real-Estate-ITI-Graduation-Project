using Document.Application.Commands.Message.MarkMessagesRead;
using Document.Application.Queries.Message.GetMessagesByConversation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _mediator.Send(new GetMessagesByConversationQuery(id, page, pageSize));
        return Ok(messages);
    }

    [HttpPut("{id}/messages/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromBody] Guid readerId)
    {
        await _mediator.Send(new MarkMessagesReadCommand(id, readerId));
        return NoContent();
    }
}