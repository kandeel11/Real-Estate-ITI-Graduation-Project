using AI.Application.DTOs;
using AI.Application.Features.Chatbot.Commands.SendMessage;
using AI.Application.Features.Chatbot.Queries.GetSessionHistory;
using AI.Application.Features.Chatbot.Queries.GetUserSessions;
using AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription;
using AI.Application.Features.PropertyDescription.Queries.GetLatestDescriptionByProperty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // بيحمي كل الـ Endpoints جوه الكنترولر بشكل افتراضي
    public class AIController : ControllerBase
    {
        // 💡 تعديل 1: اكتفينا بـ ISender بس لأنه هو الـ Base بتاع IMediator ومسؤول عن إرسال الأوامر والاستعلامات
        private readonly ISender _sender;

        public AIController(ISender sender)
        {
            _sender = sender;
        }

        #region Property Descriptions

        /// <summary>
        /// Get the latest AI-generated description draft for a property.
        /// </summary>
        [HttpGet("descriptions/by-property/{propertyId:guid}")]
        // 💡 تعديل 2: مسحنا [Authorize] المكررة لأنها موجودة فوق الكلاس كله
        public async Task<IActionResult> GetLatestDescription(Guid propertyId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetLatestDescriptionByPropertyQuery(propertyId), cancellationToken);

            if (result is null)
                return NotFound(new { Message = $"No AI description has been generated yet for property {propertyId}." });

            return Ok(result);
        }

        /// <summary>
        /// Accepts and finalizes the generated property description.
        /// </summary>
        [HttpPatch("descriptions/{id:guid}/accept")] // 💡 تعديل 3: ضفنا كلمة descriptions للراوت عشان مايحصلش تعارض مع راوتس تانية في المستقبل
        public async Task<IActionResult> AcceptDescription(
            Guid id,
            [FromBody] AcceptDescriptionRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AcceptPropertyDescriptionCommand(id, request.FinalTextEn, request.FinalTextAr);
            var result = await _sender.Send(command, cancellationToken);

            if (!result)
                return NotFound(new { Message = $"Description generation record with ID {id} not found." });

            return Ok(new { Message = "Property description has been successfully accepted." });
        }

        #endregion

        #region Standard Chatbot (Generic)

        /// <summary>
        /// Sends a message to the standard AI chatbot.
        /// </summary>
        [HttpPost("chatbot/chat")] // 💡 تعديل 4: ضفنا chatbot/ في الراوتس عشان نميزها عن الـ RAG مستقبلاً
        // 💡 تعديل 5: مسحنا [AllowAnonymous] لأن الـ Command بيستخدم _currentUserService.UserId، فلو اليوزر مش عامل لوج إن هيضرب إيرور
        public async Task<IActionResult> Chat([FromBody] SendChatBotMessageCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _sender.Send(command, cancellationToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // AI quota reached, or invalid/ended session
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets all chat sessions for the current user.
        /// </summary>
        [HttpGet("chatbot/sessions")]
        [ProducesResponseType(typeof(List<ChatbotSessionSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserSessions(CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetUserSessionsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets the message history of a specific chat session.
        /// </summary>
        [HttpGet("chatbot/sessions/{sessionId:guid}/history")]
        [ProducesResponseType(typeof(SessionHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSessionHistory([FromRoute] Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _sender.Send(new GetSessionHistoryQuery(sessionId), cancellationToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        #endregion
    }
}