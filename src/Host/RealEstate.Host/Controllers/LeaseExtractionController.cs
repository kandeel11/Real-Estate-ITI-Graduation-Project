using AI.Application.Interfaces.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaseExtractionController : ControllerBase
    {
        private readonly ILeaseExtractionService _extractionService;

        public LeaseExtractionController(ILeaseExtractionService extractionService)
        {
            _extractionService = extractionService;
        }

        [HttpPost("extract")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExtractLeaseData(IFormFile file, CancellationToken cancellationToken)
        {
            // 1. Validation
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a valid PDF file.");
            }

            if (file.ContentType != "application/pdf")
            {
                return BadRequest("Only PDF files are supported.");
            }

            try
            {
                // 2. Convert IFormFile to byte[]
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                var pdfBytes = memoryStream.ToArray();

                // 3. Call the Service
                var extractedData = await _extractionService.ExtractAsync(pdfBytes, cancellationToken);

                // 4. Return the result
                return Ok(extractedData);
            }
            catch (Exception ex)
            {
                // يُفضل إنك تستخدم Global Exception Handler، بس ده عشان التجربة السريعة
                return StatusCode(500, $"An error occurred during extraction: {ex.Message}");
            }
        }
    }
}
