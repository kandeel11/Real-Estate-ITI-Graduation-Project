using AI.Application.DTOs;
using AI.Application.Features.Chat.Commands; // RAG Commands
using AI.Application.Features.Chat.Queries;  // RAG Queries
using AI.Application.Interfaces.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RealEstate.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // 🔒 مهم جداً حماية الكنترولر ده لأن الشات هنا بيحتوي على تفاصيل عقود المستخدمين
    public class ChatRagController : ControllerBase
    {
        private readonly ISender _sender; // استخدمنا ISender لتوحيد الشكل مع AIController
        private readonly ILogger<ChatRagController> _logger;
        private readonly IContractDocumentService _ingestion;
        private readonly IIndexManagementService _indexManager;

        public ChatRagController(
            ISender sender,
            ILogger<ChatRagController> logger,
            IContractDocumentService ingestion,
            IIndexManagementService indexManager)
        {
            _sender = sender;
            _logger = logger;
            _ingestion = ingestion;
            _indexManager = indexManager;
        }

        [HttpGet("ping")]
        [AllowAnonymous] // دي الوحيدة اللي مسموح للكل ينده عليها عشان الـ Health Check
        public IActionResult Ping() => Ok("alive");

        #region Admin & Document Ingestion

        /// <summary>
        /// Upload a contract PDF — extracts, chunks, embeds and stores in Qdrant.
        /// </summary>
        [HttpPost("ingest")]
        // 💡 يفضل مستقبلاً إضافة [Authorize(Roles = "Admin")] هنا
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

        [HttpPost("admin/fix-indexes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FixIndexesAsync(
               [FromQuery] string collectionName = "contracts",
               CancellationToken cancellationToken = default)
        {
            await _indexManager.EnsureIndexesAsync(collectionName, cancellationToken);
            return Ok($"Indexes created ✅ on '{collectionName}' — test /api/ChatRag/message now");
        }

        #endregion

        #region RAG Chat Operations

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
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get conversation history for a RAG session.
        /// </summary>
        [HttpGet("sessions/{sessionId:guid}/history")] // 💡 خلينا الراوت RESTful ومطابق للشات العادي
        [ProducesResponseType(typeof(IReadOnlyList<SessionMessage>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryAsync(
            [FromRoute] Guid sessionId, // 💡 تعديل مهم: غيرناها من string لـ Guid
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetSessionHistoryQuery(sessionId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Clear conversation history for a RAG session.
        /// </summary>
        [HttpDelete("sessions/{sessionId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ClearHistoryAsync(
            [FromRoute] Guid sessionId, // 💡 تعديل مهم: غيرناها من string لـ Guid
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new ClearSessionCommand(sessionId), cancellationToken);
            return NoContent();
        }

        #endregion
    }
}