using AI.Application.DTOs;
using AI.Application.Features.Chat.Commands;
using AI.Application.Features.Chat.Queries;
using AI.Application.Interfaces.Common;
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
        private readonly IContractDocumentService _ingestion;
        public ChatRagController(IMediator mediator, ILogger<ChatRagController> logger, IContractDocumentService ingestion)
        {
            _mediator = mediator;
            _logger = logger;
            _ingestion = ingestion;
        }
        /// <summary>
        /// Upload a contract PDF — extracts, chunks, embeds and stores in Qdrant.
        /// </summary>
        [HttpPost("ingest")]
        [ProducesResponseType(typeof(ContractIngestionResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IngestAsync(
            IFormFile file,
            [FromQuery] Guid contractId,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return BadRequest("No PDF file provided.");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are supported.");

            await using var stream = file.OpenReadStream();

            var result = await _ingestion.IngestAsync(
                pdfStream: stream,
                request: new ContractIngestionRequest(
                    ContractId: contractId,
                    CollectionName: "contracts",
                    FileName: file.FileName),
                cancellationToken: cancellationToken);

            return Ok(result);
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
