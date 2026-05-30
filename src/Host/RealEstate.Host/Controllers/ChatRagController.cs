using AI.Application.DTOs;
using AI.Application.Features.Chat.Commands;
using AI.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ChatRagController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChatRagController> _logger;
        public ChatRagController(IMediator mediator, ILogger<ChatRagController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        /// <summary>
        /// Send a message — AI auto-searches contracts and responds.
        /// </summary>
        [HttpPost("message")]
        [ProducesResponseType(typeof(SendMessageResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendMessageAsync(
            [FromBody] SendMessageCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get conversation history for a session.
        /// </summary>
        [HttpGet("history/{sessionId}")]
        [ProducesResponseType(typeof(IReadOnlyList<SessionMessage>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryAsync(
            string sessionId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetSessionHistoryQuery(sessionId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Clear conversation history for a session.
        /// </summary>
        [HttpDelete("history/{sessionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearHistoryAsync(
            string sessionId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new ClearSessionCommand(sessionId), cancellationToken);
            return NoContent();
        }



    }
}
