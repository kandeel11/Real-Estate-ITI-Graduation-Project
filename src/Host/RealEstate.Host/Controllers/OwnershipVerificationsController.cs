using AI.Application.Features.OwnershipVerification.Commands.AdminApproveVerification;
using AI.Application.Features.OwnershipVerification.Commands.AdminRejectVerification;
using AI.Application.Features.OwnershipVerification.Commands.SubmitVerification;
using AI.Application.Features.OwnershipVerification.DTOs;
using AI.Application.Features.OwnershipVerification.Queries.GetPendingAdminReviews;
using AI.Application.Features.OwnershipVerification.Queries.GetVerificationStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RealEstate.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class OwnershipVerificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OwnershipVerificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Submit a new ownership verification request with National ID and ownership proof documents.
        /// </summary>
        [HttpPost("submit")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SubmitVerification(
            [FromForm] SubmitVerificationRequest request,
            CancellationToken cancellationToken
        )
        {
            // Validate files
            if (request.NationalIdFile == null || request.NationalIdFile.Length == 0)
                return BadRequest("National ID file is required");

            if (request.OwnershipProofFile == null || request.OwnershipProofFile.Length == 0)
                return BadRequest("Ownership proof file is required");

            // Read file contents
            byte[] nationalIdBytes, ownershipProofBytes;

            using (var ms = new MemoryStream())
            {
                await request.NationalIdFile.CopyToAsync(ms, cancellationToken);
                nationalIdBytes = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                await request.OwnershipProofFile.CopyToAsync(ms, cancellationToken);
                ownershipProofBytes = ms.ToArray();
            }

            var command = new SubmitOwnershipVerificationCommand(
     PropertyId: request.PropertyId,
     OwnerId: request.OwnerId,
     OwnerName: request.OwnerName,
     NationalIdFile: nationalIdBytes,
     NationalIdMimeType: request.NationalIdFile.ContentType ?? "image/jpeg",
     OwnershipProofFile: ownershipProofBytes,
     OwnershipProofMimeType: request.OwnershipProofFile.ContentType ?? "application/pdf",

     // 👇 قم بتمرير المتغيرات الجديدة هنا
     Country: request.Country,
     Governorate: request.Governorate,
     City: request.City,
     District: request.District,
     Address: request.Address
 );

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get verification status by request ID.
        /// </summary>
        [HttpGet("{verificationRequestId}")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatus(
            Guid verificationRequestId,
            CancellationToken cancellationToken
        )
        {
            var query = new GetVerificationStatusQuery(verificationRequestId);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Get all pending admin reviews.
        /// </summary>
        [HttpGet("admin/pending")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<VerificationStatusDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingReviews(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default
        )
        {
            var query = new GetPendingAdminReviewsQuery(limit);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin approves an ownership verification (moves to ManuallyVerified status).
        /// </summary>
        [HttpPost("{verificationRequestId}/approve")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveVerification(
            Guid verificationRequestId,
            [FromBody] AdminReviewRequest request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Extract admin ID from current user claims
            var adminId = Guid.NewGuid(); // Placeholder

            var command = new AdminApproveVerificationCommand(
                VerificationRequestId: verificationRequestId,
                AdminId: adminId,
                ReviewNotes: request.ReviewNotes
            );

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Admin rejects an ownership verification.
        /// </summary>
        [HttpPost("{verificationRequestId}/reject")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectVerification(
            Guid verificationRequestId,
            [FromBody] AdminReviewRequest request,
            CancellationToken cancellationToken
        )
        {
            // TODO: Extract admin ID from current user claims
            var adminId = Guid.NewGuid(); // Placeholder

            var command = new AdminRejectVerificationCommand(
                VerificationRequestId: verificationRequestId,
                AdminId: adminId,
                RejectionReason: request.ReviewNotes
            );

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for submitting ownership verification.
    /// </summary>
    /// <summary>
    /// Request model for submitting ownership verification.
    /// </summary>
    public class SubmitVerificationRequest
    {
        public Guid PropertyId { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; }
        public IFormFile NationalIdFile { get; set; }
        public IFormFile OwnershipProofFile { get; set; }

        // 👇 أضف هذه الخصائص الجديدة
        public string Country { get; set; }
        public string Governorate { get; set; }
        public string City { get; set; }
        public string? District { get; set; }
        public string Address { get; set; }
    }
    /// <summary>
    /// Request model for admin review (approve/reject).
    /// </summary>
    public class AdminReviewRequest
    {
        public string ReviewNotes { get; set; }
    }
}
