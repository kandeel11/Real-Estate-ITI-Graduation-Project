using AI.Application.DTOs;
using AI.Application.Features.Chatbot.Commands.SendMessage;
using AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription;
using AI.Application.Features.PropertyDescription.Queries.GetLatestDescriptionByProperty;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly ISender _sender;

        public AIController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Get the latest AI-generated description draft for a property (for review before accepting).
        /// Returns 404 if no description has been generated yet.
        /// </summary>
        [HttpGet("descriptions/by-property/{propertyId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetLatestDescription(
            Guid propertyId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetLatestDescriptionByPropertyQuery(propertyId), cancellationToken);

            if (result is null)
                return NotFound(new { Message = $"No AI description has been generated yet for property {propertyId}." });

            return Ok(result);
        }

        [HttpPatch("{id:guid}/accept")]
        [Authorize]
        public async Task<IActionResult> AcceptDescription(
     Guid id,
     [FromBody] AcceptDescriptionRequest request, // Receives the edited texts from the UI
     CancellationToken cancellationToken)
        {
            // Pass the ID from the route, and the texts from the request body
            var command = new AcceptPropertyDescriptionCommand(
                id,
                request.FinalTextEn,
                request.FinalTextAr
            );

            var result = await _sender.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { Message = $"Description generation record with ID {id} not found." });
            }

            return Ok(new { Message = "Property description has been successfully accepted." });
        }

        [HttpPost("chat")]
        [AllowAnonymous]
        public async Task<IActionResult> Chat([FromBody] SendMessageCommand command, CancellationToken cancellationToken)
        {
            // Dispatch the command to the MediatR pipeline
            var response = await _sender.Send(command, cancellationToken);

            // Return HTTP 200 OK containing the session ID and the AI's reply
            return Ok(response);
        }
    }
}
