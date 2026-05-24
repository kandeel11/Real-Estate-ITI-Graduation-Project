using AI.Application.Features.PropertyDescription.Commands.AcceptPropertyDescription;
using AI.Application.Features.PropertyDescription.Commands.GeneratePropertyDescription;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Controllers : ControllerBase
    {
        private readonly ISender _sender;

        public Controllers(ISender sender)
        {
            _sender = sender;
        }
        [HttpPost("generate-property-description")]
        public async Task<IActionResult> GeneratePropertyDescription(
        [FromBody] GeneratePropertyDescriptionCommand command,
        CancellationToken cancellationToken)
        {
            var response = await _sender.Send(command, cancellationToken);
            return Ok(response);
        }
        [HttpPatch("{id:guid}/accept")]
        public async Task<IActionResult> AcceptDescription(Guid id, CancellationToken cancellationToken)
        {
            var command = new AcceptPropertyDescriptionCommand(id);
            var result = await _sender.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound(new { Message = $"Description generation record with ID {id} not found." });
            }

            return Ok(new { Message = "Property description has been successfully accepted." });
        }
    }
}
